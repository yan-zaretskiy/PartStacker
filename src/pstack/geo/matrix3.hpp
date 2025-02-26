#ifndef PSTACK_GEO_MATRIX3_HPP
#define PSTACK_GEO_MATRIX3_HPP

#include "pstack/geo/functions.hpp"

namespace pstack::geo {

template <class T>
struct matrix3 {
    T xx, xy, xz;
    T yx, yy, yz;
    T zx, zy, zz;
};

template <class T>
constexpr matrix3<T> operator*(const matrix3<T>& lhs, const matrix3<T>& rhs) {
    matrix3<T> m;
    m.xx = (lhs.xx * rhs.xx) + (lhs.xy * rhs.yx) + (lhs.xz * rhs.zx);
    m.xy = (lhs.xx * rhs.xy) + (lhs.xy * rhs.yy) + (lhs.xz * rhs.zy);
    m.xz = (lhs.xx * rhs.xz) + (lhs.xy * rhs.yz) + (lhs.xz * rhs.zz);
    m.yx = (lhs.yx * rhs.xx) + (lhs.yy * rhs.yx) + (lhs.yz * rhs.zx);
    m.yy = (lhs.yx * rhs.xy) + (lhs.yy * rhs.yy) + (lhs.yz * rhs.zy);
    m.yz = (lhs.yx * rhs.xz) + (lhs.yy * rhs.yz) + (lhs.yz * rhs.zz);
    m.zx = (lhs.zx * rhs.xx) + (lhs.zy * rhs.yx) + (lhs.zz * rhs.zx);
    m.zy = (lhs.zx * rhs.xy) + (lhs.zy * rhs.yy) + (lhs.zz * rhs.zy);
    m.zz = (lhs.zx * rhs.xz) + (lhs.zy * rhs.yz) + (lhs.zz * rhs.zz);
    return m;
}

template <class T>
inline constexpr matrix3<T> eye3 = { 1,  0,  0,
                                     0,  1,  0,
                                     0,  0,  1 };

template <class T>
constexpr matrix3<T> rot3_x(const T theta) {
    const T c = static_cast<T>(cos(theta));
    const T s = static_cast<T>(sin(theta));
    return { 1,  0,  0,
             0,  c, -s,
             0,  s,  c };
}

template <class T>
constexpr matrix3<T> rot3_y(const T theta) {
    const T c = static_cast<T>(cos(theta));
    const T s = static_cast<T>(sin(theta));
    return { c,  0,  s,
             0,  1,  0,
            -s,  0,  c };
}

template <class T>
constexpr matrix3<T> rot3_z(const T theta) {
    const T c = static_cast<T>(cos(theta));
    const T s = static_cast<T>(sin(theta));
    return { c, -s,  0,
             s,  c,  0,
             0,  0,  1 };
}

template <class T>
constexpr matrix3<T> scale3(const T x, const T y, const T z) {
    return { x,  0,  0,
             0,  y,  0,
             0,  0,  z };
}

} // namespace pstack::geo

#endif // PSTACK_GEO_MATRIX3_HPP
