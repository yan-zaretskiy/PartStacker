#include "pstack/calc/stacker.hpp"
#include "pstack/geo/mesh.hpp"
#include <optional>

namespace pstack::calc {

namespace {

std::optional<geo::mesh> stack_impl(const stacker_parameters& params) {
    (void)params;
    return std::nullopt;
}

} // namespace

void stacker::stack(const stacker_parameters params) {
    if (_running.exchange(true)) {
        return;
    }
    std::optional<geo::mesh> result = stack_impl(params);
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
