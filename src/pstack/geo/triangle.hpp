#ifndef PSTACK_GEO_TRIANGLE_HPP
#define PSTACK_GEO_TRIANGLE_HPP

#include "pstack/geo/point3.hpp"
#include "pstack/geo/vector3.hpp"
#include <type_traits>

namespace pstack::geo {

struct triangle {
    vector3<float> normal;
    point3<float> v1, v2, v3;
};

static_assert(std::is_trivially_copyable_v<triangle>);

} // namespace pstack::geo

#endif // PSTACK_GEO_TRIANGLE_HPP
