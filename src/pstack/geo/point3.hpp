#ifndef PSTACK_GEO_POINT3_HPP
#define PSTACK_GEO_POINT3_HPP

#include "pstack/geo/vector3.hpp"
#include <algorithm>

namespace pstack::geo {

template <class T>
struct point3 {
    T x;
    T y;
    T z;

    constexpr vector3<T> as_vector() const {
        return { x, y, z };
    }
};

template <class T>
inline constexpr point3<T> origin3 = { 0, 0, 0 };

template <class T>
constexpr point3<T> min(const point3<T> lhs, const point3<T> rhs) {
    return { std::min(lhs.x, rhs.x), std::min(lhs.y, rhs.y), std::min(lhs.z, rhs.z) };
}

template <class T>
constexpr point3<T> max(const point3<T> lhs, const point3<T> rhs) {
    return { std::max(lhs.x, rhs.x), std::max(lhs.y, rhs.y), std::max(lhs.z, rhs.z) };
}

template <class T>
constexpr point3<T> operator+(const point3<T> lhs, const vector3<T> rhs) {
    return { lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z };
}

template <class T>
constexpr point3<T> operator+(const vector3<T> lhs, const point3<T> rhs) {
    return { lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z };
}

template <class T>
constexpr vector3<T> operator-(const point3<T> lhs, const point3<T> rhs) {
    return { lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z };
}

template <class T>
constexpr point3<T>& operator+=(point3<T>& lhs, const vector3<T>& rhs) {
    lhs = { lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z };
    return lhs;
}

template <class T>
constexpr point3<T> operator+(const point3<T>& lhs, const std::type_identity_t<T>& rhs) {
    return { lhs.x + rhs, lhs.y + rhs, lhs.z + rhs };
}

template <class T>
constexpr point3<T> operator+(const std::type_identity_t<T>& lhs, const vector3<T>& rhs) {
    return { lhs + rhs.x, lhs + rhs.y, lhs + rhs.z };
}

template <class T>
constexpr point3<T> operator+(const point3<T> lhs, const point3<T> rhs) {
    return { lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z };
}

template <class T>
constexpr point3<T> operator-(const point3<T>& lhs, const std::type_identity_t<T>& rhs) {
    return { lhs.x - rhs, lhs.y - rhs, lhs.z - rhs };
}

template <class T>
constexpr point3<T> operator-(const std::type_identity_t<T>& lhs, const vector3<T>& rhs) {
    return { lhs - rhs.x, lhs - rhs.y, lhs - rhs.z };
}


} // namespace pstack::geo

#endif // PSTACK_GEO_POINT3_HPP
