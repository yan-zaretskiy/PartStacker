#include "pstack/calc/sinterbox.hpp"
#include <mdspan>
#include <vector>

namespace pstack::calc {

namespace {

void append_side(std::vector<geo::triangle>& triangles, const std::mdspan<const geo::point3<float>, std::dextents<std::size_t, 2>> points, const geo::vector3<float>& normal, const geo::vector3<float>& dir1, const geo::vector3<float>& dir2, double thickness_value) {
    const auto thickness = normal * -thickness_value;

    const bool condition = normal.x + normal.y + normal.z > 0;
    const auto make_triangle = [condition](const geo::vector3<float>& n, const geo::point3<float>& v1, const geo::point3<float>& v2, const geo::point3<float>& v3) {
        if (condition) {
            return geo::triangle{ n, v1, v2, v3 };
        } else {
            return geo::triangle{ n, v1, v3, v2 };
        }
    };

    for (std::size_t i = 0; i < points.extent(0) - 1; ++i) {
        for (std::size_t j = 0; j < points.extent(1) - 1; ++j) {
            const auto& p00 = points[i, j];
            const auto& p01 = points[i, j + 1];
            const auto& p10 = points[i + 1, j];
            const auto& p11 = points[i + 1, j + 1];
            if (i % 2 == 1 && j % 2 == 1) {
                triangles.push_back(make_triangle(dir2, p00, p10, p00 + thickness));
                triangles.push_back(make_triangle(dir2, p10 + thickness, p00 + thickness, p10));
                triangles.push_back(make_triangle(-dir2, p01, p01 + thickness, p11));
                triangles.push_back(make_triangle(-dir2, p11 + thickness, p11, p01 + thickness));
                // Flip i and j, and reverse the order
                triangles.push_back(make_triangle(dir1, p00, p00 + thickness, p01));
                triangles.push_back(make_triangle(dir1, p01 + thickness, p01, p00 + thickness));
                triangles.push_back(make_triangle(-dir1, p10, p11, p10 + thickness));
                triangles.push_back(make_triangle(-dir1, p11 + thickness, p10 + thickness, p11));
            } else {
                if (i != 0 && j != 0 && i != points.extent(0) - 2 && j != points.extent(1) - 2) {
                    triangles.push_back(make_triangle(normal, p00, p10, p01));
                    triangles.push_back(make_triangle(normal, p11, p01, p10));
                }
                triangles.push_back(make_triangle(-normal, p00 + thickness, p01 + thickness, p10 + thickness));
                triangles.push_back(make_triangle(-normal, p11 + thickness, p10 + thickness, p01 + thickness));
            }
        }
    }
}

std::tuple<std::vector<float>, std::vector<float>, std::vector<float>> make_positions(const sinterbox_parameters& params) {
    const auto [min, max, clearance, thickness, width, desired_spacing] = params;
    const geo::vector3 size = max - min;

    // Number of bars in the given direction
    const geo::vector3 N = [&] {
        auto n = (size + (2 * clearance) - desired_spacing) / (width + desired_spacing);
        return geo::vector3{ (float)(int)n.x, (float)(int)n.y, (float)(int)n.z }; // Round down
    }();

    const geo::vector3 _numerators = size + (2 * clearance) - (N * width);
    const geo::vector3 real_spacing = {
        _numerators.x / (N.x + 1),
        _numerators.y / (N.y + 1),
        _numerators.z / (N.z + 1)
    };

    const geo::point3 lower_bound = min - clearance;
    const geo::point3 upper_bound = max + clearance;

    const auto make_vector = [&](auto elem) -> std::vector<float> {
        std::vector<float> out;
        out.reserve(4 * (int)elem(N) * 2);
        out.push_back(elem(lower_bound) - thickness);
        for (double pos = elem(lower_bound); pos <= elem(upper_bound); pos += (elem(real_spacing) + width)) {
            out.push_back(pos);
            out.push_back(pos + elem(real_spacing));
        }
        out.push_back(elem(upper_bound) + thickness);
        return out;
    };

    return {
        make_vector([](const auto& v) { return v.x; }),
        make_vector([](const auto& v) { return v.y; }),
        make_vector([](const auto& v) { return v.z; })
    };
}

} // namespace

void append_sinterbox(std::vector<geo::triangle>& triangles, const sinterbox_parameters& params) {
    const auto [positions_x, positions_y, positions_z] = make_positions(params);
    const geo::point3 lower_bound = { positions_x.at(1), positions_y.at(1), positions_z.at(1) };
    const geo::point3 upper_bound = { positions_x.at(positions_x.size() - 2), positions_y.at(positions_y.size() - 2), positions_z.at(positions_z.size() - 2) };

    // XY sides
    {
        std::vector<geo::point3<float>> lower_xy_data(positions_x.size() * positions_y.size(), {});
        std::vector<geo::point3<float>> upper_xy_data(positions_x.size() * positions_y.size(), {});
        std::mdspan lower_xy(lower_xy_data.data(), positions_x.size(), positions_y.size());
        std::mdspan upper_xy(upper_xy_data.data(), positions_x.size(), positions_y.size());
        for (std::size_t x = 0; x < positions_x.size(); ++x) {
            for (std::size_t y = 0; y < positions_y.size(); ++y) {
                lower_xy[x, y] = { positions_x[x], positions_y[y], lower_bound.z };
                upper_xy[x, y] = { positions_x[x], positions_y[y], upper_bound.z };
            }
        }
        append_side(triangles, lower_xy, geo::unit_z<float>, geo::unit_x<float>, geo::unit_y<float>, params.thickness);
        append_side(triangles, upper_xy, -geo::unit_z<float>, geo::unit_x<float>, geo::unit_y<float>, params.thickness);
    }

    // ZX sides
    // Note, doing "XZ" instead of "ZX" makes the triangles face the wrong way
    {
        std::vector<geo::point3<float>> lower_zx_data(positions_z.size() * positions_x.size(), {});
        std::vector<geo::point3<float>> upper_zx_data(positions_z.size() * positions_x.size(), {});
        std::mdspan lower_zx(lower_zx_data.data(), positions_z.size(), positions_x.size());
        std::mdspan upper_zx(upper_zx_data.data(), positions_z.size(), positions_x.size());
        for (std::size_t z = 0; z < positions_z.size(); ++z) {
            for (std::size_t x = 0; x < positions_x.size(); ++x) {
                lower_zx[z, x] = { positions_x[x], lower_bound.y, positions_z[z] };
                upper_zx[z, x] = { positions_x[x], upper_bound.y, positions_z[z] };
            }
        }
        append_side(triangles, lower_zx, geo::unit_y<float>, geo::unit_z<float>, geo::unit_x<float>, params.thickness);
        append_side(triangles, upper_zx, -geo::unit_y<float>, geo::unit_z<float>, geo::unit_x<float>, params.thickness);
    }

    // YZ sides
    {
        std::vector<geo::point3<float>> lower_yz_data(positions_y.size() * positions_z.size(), {});
        std::vector<geo::point3<float>> upper_yz_data(positions_y.size() * positions_z.size(), {});
        std::mdspan lower_yz(lower_yz_data.data(), positions_y.size(), positions_z.size());
        std::mdspan upper_yz(upper_yz_data.data(), positions_y.size(), positions_z.size());
        for (std::size_t y = 0; y < positions_y.size(); ++y) {
            for (std::size_t z = 0; z < positions_z.size(); ++z) {
                lower_yz[y, z] = { lower_bound.x, positions_y[y], positions_z[z] };
                upper_yz[y, z] = { upper_bound.x, positions_y[y], positions_z[z] };
            }
        }
        append_side(triangles, lower_yz, geo::unit_x<float>, geo::unit_y<float>, geo::unit_z<float>, params.thickness);
        append_side(triangles, upper_yz, -geo::unit_x<float>, geo::unit_y<float>, geo::unit_z<float>, params.thickness);
    }
}

} // namespace pstack::calc
