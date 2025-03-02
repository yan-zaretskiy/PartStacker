#include "pstack/calc/bool.hpp"
#include "pstack/calc/mesh.hpp"
#include "pstack/calc/rotations.hpp"
#include "pstack/calc/stacker.hpp"
#include "pstack/calc/voxelize.hpp"
#include "pstack/util/mdarray.hpp"
#include <algorithm>
#include <optional>
#include <ranges>

namespace pstack::calc {

namespace {

struct stack_state {
    struct mesh_entry {
        mesh mesh;
        mesh::bounding_t bounding;
    };

    std::vector<std::vector<mesh_entry>> meshes;
    std::vector<util::mdarray<int, 3>> voxels;
    util::mdarray<Bool, 3> space;
    std::vector<part_properties> ordered_parts;
    mesh result;
};

void place(const util::mdspan<Bool, 3> space, const int index, const util::mdspan<const int, 3> obj, const int x, const int y, const int z) {
    const int max_i = std::min(x + obj.extent(0), space.extent(0));
    const int max_j = std::min(y + obj.extent(1), space.extent(1));
    const int max_k = std::min(z + obj.extent(2), space.extent(2));
    for (int i = x; i < max_i; ++i) {
        for (int j = y; j < max_j; ++j) {
            for (int k = z; k < max_k; ++k) {
                space[i, j, k] |= (obj[i - x, j - y, k - z] & index) != 0;
            }
        }
    }
}

int can_place(const util::mdspan<const Bool, 3> space, int possible, const util::mdspan<const int, 3> obj, const int x, const int y, const int z) {
    const int max_i = std::min(x + obj.extent(0), space.extent(0));
    const int max_j = std::min(y + obj.extent(1), space.extent(1));
    const int max_k = std::min(z + obj.extent(2), space.extent(2));
    for (int i = x; i < max_i; ++i) {
        for (int j = y; j < max_j; ++j) {
            for (int k = z; k < max_k; ++k) {
                if (space[i, j, k]) {
                    possible &= (possible ^ obj[i - x, j - y, k - z]);
                    if (possible == 0) {
                        return 0;
                    }
                }
            }
        }
    }
    return possible;
}

int try_place(const stacker_parameters& params, stack_state& state, int p, const geo::point3<int> max, int total_parts, int& current_count) {
    for (int s = 0; s <= max.x + max.y + max.z; ++s) {
        for (int r = std::max(0, s - max.z); r <= std::min(s, max.x + max.y); ++r) {
            const int z = s - r;
            for (int x = std::max(0, r - max.y); x <= std::min(r, max.x); ++x) {
                const int y = r - x;
                const std::span rotations = rotation_sets[state.ordered_parts[p].rotation_index];

                // Calculate which orientations fit in bounding box
                int bit_index = 1;
                int possible = 0;
                for (int i = 0; i < rotations.size(); ++i) {
                    const auto& box_size = state.meshes[p][i].bounding.box_size;
                    if (x + box_size.x < max.x && y + box_size.y < max.y && z + box_size.z < max.z) {
                        possible |= bit_index;
                    }
                    bit_index *= 2;
                }

                possible = can_place(state.space, possible, state.voxels[p], x, y, z);

                if (possible != 0) { // If it fits, figure out which rotation to use
                    bit_index = 1;
                    for (int i = 0; i < rotations.size(); ++i) {
                        if ((possible & bit_index) == 0) {
                            bit_index *= 2;
                            continue;
                        } else {
                            state.result.add(state.meshes[p][i].mesh, { (float)x, (float)y, (float)z });
                            place(state.space, bit_index, state.voxels[p], x, y, z); // Mark voxels as occupied

                            ++current_count;
                            params.set_progress(current_count, total_parts);
                            params.display_mesh(state.result, max.x, max.y, max.z);

                            --state.ordered_parts[p].quantity; // Move to next instance of part
                            if (state.ordered_parts[p].quantity == 0) { // All instances placed, try next part
                                p--;
                                if (p < 0) {
                                    return -1;
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }
    }

    // Reached the end of the box, return the part we're currently at.
    return p;
}


std::optional<mesh> stack_impl(const stacker_parameters& params, const std::atomic<bool>& running) {
    stack_state state{};
    {
        auto parts = params.parts;
        std::ranges::sort(parts, std::greater{}, &part_properties::volume);
        state.ordered_parts.reserve(parts.size());
        for (const part_properties* part : parts) {
            state.ordered_parts.push_back(*part);
        }
    }
    state.meshes.assign(state.ordered_parts.size(), {});
    state.voxels.assign(state.ordered_parts.size(), {});

    double triangles = 0;
    const double scale_factor = 1 / params.resolution;
    int total_parts = 0;
    for (const part_properties& part : state.ordered_parts) {
        triangles += part.triangle_count * rotation_sets[part.rotation_index].size();
        total_parts += part.quantity;
    }

    double progress = 0;
    for (int i = 0; i < state.ordered_parts.size(); ++i) {
        geo::matrix3 base_rotation = geo::eye3<float>;

        if (state.ordered_parts[i].rotate_min_box) {
            mesh reduced_mesh{ state.ordered_parts[i].mesh.triangles()
                             | std::views::stride(15)
                             | std::ranges::to<std::vector>() };

            static constexpr std::size_t sections = 20;
            static constexpr double angle_diff = 2 * geo::pi / sections;

            static constexpr geo::matrix3 rot_x = geo::rot3_x<float>(angle_diff);
            static constexpr geo::matrix3 rot_y = geo::rot3_y<float>(angle_diff);

            int min_box_volume = std::numeric_limits<int>::max();
            double best_x = 0;
            double best_y = 0;

            for (double x = 0; x < 2 * geo::pi; x += angle_diff) {
                reduced_mesh.rotate(rot_x);
                for (double y = 0; y < 2 * geo::pi; y += angle_diff) {
                    reduced_mesh.rotate(rot_y);
                    const auto box = reduced_mesh.bounding().box_size;
                    const int box_volume = box.x * box.y * box.z;
                    if (box_volume < min_box_volume) {
                        min_box_volume = box_volume;
                        best_x = x;
                        best_y = y;
                    }
                }
            }

            base_rotation = geo::rot3_y<float>(best_y) * geo::rot3_x<float>(best_x);
        }

        // Set up array of parts
        const std::span rotations = rotation_sets[state.ordered_parts[i].rotation_index];
        state.meshes[i].reserve(rotations.size());

        // Track bounding box size
        geo::vector3<int> max_box_size = { 1, 1, 1 };

        // Calculate all the rotations
        for (int j = 0; j < rotations.size(); ++j) {
            if (not running) {
                return std::nullopt;
            }

            mesh m = state.ordered_parts[i].mesh;
            m.scale(scale_factor);
            m.rotate(base_rotation * rotations[j]);
            m.set_baseline({ 0, 0, 0 });

            auto bounding = m.bounding();
            const auto box_size = bounding.box_size;
            max_box_size.x = std::max(box_size.x, max_box_size.x);
            max_box_size.y = std::max(box_size.y, max_box_size.y);
            max_box_size.z = std::max(box_size.z, max_box_size.z);

            state.meshes[i].emplace_back(std::move(m), std::move(bounding));

            progress += state.ordered_parts[i].triangle_count / 2;
            params.set_progress(progress, triangles);
        }

        // Initialize space size to appropriate dimensions
        state.voxels[i] = { max_box_size.x, max_box_size.y, max_box_size.z };

        // Voxelize each rotated instance of this part
        int bit_index = 1;
        for (const auto& [mesh, _] : state.meshes[i]) {
            if (not running) {
                return std::nullopt;
            }

            voxelize(mesh, state.voxels[i], bit_index, state.ordered_parts[i].min_hole);
            bit_index *= 2;

            progress += state.ordered_parts[i].triangle_count / 2;
            params.set_progress(progress, triangles);
        }
    }

    int max_x = static_cast<int>(scale_factor * params.x_min);
    int max_y = static_cast<int>(scale_factor * params.y_min);
    int max_z = static_cast<int>(scale_factor * params.z_min);
    state.space = {
        std::max(max_x, static_cast<int>(scale_factor * params.x_max)),
        std::max(max_y, static_cast<int>(scale_factor * params.y_max)),
        std::max(max_z, static_cast<int>(scale_factor * params.z_max))
    };

    params.set_progress(0, 1);

    int current_count = 0;
    int part_index = state.meshes.size() - 1;
    while (part_index >= 0) {
        const int old_part_index = part_index;

        part_index = try_place(params, state, part_index, { max_x, max_y, max_z }, total_parts, current_count);
        if (part_index < 0) {
            break; // Done!
        }

        // If pIndex has not changed it means there are no more ways to place an instance of the current part in the box: it must be enlarged
        if (part_index == old_part_index) {
            int best = std::numeric_limits<int>::max();
            int new_x = state.space.extent(0);
            int new_y = state.space.extent(1);
            int new_z = state.space.extent(2);

            int min_box_x = std::numeric_limits<int>::max();
            int min_box_y = std::numeric_limits<int>::max();
            int min_box_z = std::numeric_limits<int>::max();
            for (const auto& [_, bounding] : state.meshes[part_index]) {
                min_box_x = std::min(bounding.box_size.x, min_box_x);
                min_box_y = std::min(bounding.box_size.y, min_box_y);
                min_box_z = std::min(bounding.box_size.z, min_box_z);
            }

            for (int s = 0; s < state.space.extent(0) + state.space.extent(1) + state.space.extent(2) - min_box_x - min_box_y - min_box_z; ++s) {
                for (int r = std::max<std::size_t>(0, s - state.space.extent(2) - min_box_z); r <= std::min<std::size_t>(s, state.space.extent(0) + state.space.extent(1) - min_box_x - min_box_y); ++r) {
                    const int z = s - r;
                    if (std::max(z + min_box_z, max_z) * max_y * max_x > best) {
                        break;
                    }

                    for (int x = std::max<std::size_t>(0, r - state.space.extent(1) - min_box_y); x <= std::min<std::size_t>(r, state.space.extent(0) - min_box_z); ++x) {
                        const int y = r - x;
                        if (std::max(x + min_box_x, max_x) * std::max(y + min_box_y, max_y) * std::max(z + min_box_z, max_z) > best) {
                            continue;
                        }

                        const std::span rotations = rotation_sets[state.ordered_parts[part_index].rotation_index];

                        // Calculate which orientations fit in bounding box
                        int bit_index = 1;
                        int possible = 0;
                        for (int i = 0; i < rotations.size(); ++i) {
                            const auto& box_size = state.meshes[part_index][i].bounding.box_size;
                            if (x + box_size.x < state.space.extent(0) && y + box_size.y < state.space.extent(1) && z + box_size.z < state.space.extent(2)) {
                                possible |= bit_index;
                            }
                            bit_index *= 2;
                        }
                                
                        possible = can_place(state.space, possible, state.voxels[part_index], x, y, z);

                        if (possible != 0) { // If it fits, figure out which rotation to use
                            bit_index = 1;
                            for (int i = 0; i < rotations.size(); ++i) {
                                if ((possible & bit_index) != 0) {
                                    const auto& box_size = state.meshes[part_index][i].bounding.box_size;
                                    const int new_box = std::max(max_x, x + box_size.x) * std::max(max_y, y + box_size.y) * std::max(max_z, z + box_size.z);
                                    if (new_box < best) {
                                        best = new_box;
                                        new_x = x + box_size.x;
                                        new_y = y + box_size.y;
                                        new_z = z + box_size.z;
                                    }
                                }
                                bit_index *= 2;
                            }
                        }
                    }
                }
            }

            if (best == std::numeric_limits<int>::max()) {
                return mesh{};
            }

            max_x = std::max(max_x, new_x + 2);
            max_y = std::max(max_y, new_y + 2);
            max_z = std::max(max_z, new_z + 2);
        }

        if (not running) {
            return std::nullopt;
        }
    }

    state.result.scale(1 / scale_factor);
    return { std::move(state.result) };
}

} // namespace

void stacker::stack(const stacker_parameters params) {
    if (_running.exchange(true)) {
        return;
    }
    std::optional<mesh> result = stack_impl(params, _running);
    if (result.has_value()) {
        if (result->triangles().empty()) {
            params.on_failure();
        } else {
            params.on_success(std::move(*result));
        }
    }
    params.on_finish();
    _running = false;
}

} // namespace pstack::calc
