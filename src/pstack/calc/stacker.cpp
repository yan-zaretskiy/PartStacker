#include "pstack/calc/mesh.hpp"
#include "pstack/calc/stacker.hpp"
#include "pstack/util/mdarray.hpp"
#include <algorithm>
#include <optional>

namespace pstack::calc {

namespace {

class Bool {
private:
    bool value;
public:
    Bool() : value() {}
    Bool(bool b) : value(b) {}
    operator bool&() & {
        return value;
    }
    operator bool() const {
        return value;
    }
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

std::optional<mesh> stack_impl(const stacker_parameters& params) {
    (void)params;
    return std::nullopt;
}

} // namespace

void stacker::stack(const stacker_parameters params) {
    if (_running.exchange(true)) {
        return;
    }
    std::optional<mesh> result = stack_impl(params);
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
