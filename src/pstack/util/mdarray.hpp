#ifndef PSTACK_UTIL_MDARRAY_HPP
#define PSTACK_UTIL_MDARRAY_HPP

#include <type_traits>
#include <vector>

#if defined(__cpp_lib_mdspan) and __cpp_lib_mdspan >= 202207L
#include <mdspan>
#else
#include "mdspan/mdspan.hpp"
#endif

namespace pstack::util {

#if defined(__cpp_lib_mdspan) and __cpp_lib_mdspan >= 202207L
template <class T, std::size_t Rank>
using mdspan = std::mdspan<T, std::dextents<std::size_t, Rank>>;
using std::extents;
#else
template <class T, std::size_t Rank>
using mdspan = MDSPAN_IMPL_STANDARD_NAMESPACE::mdspan<T, MDSPAN_IMPL_STANDARD_NAMESPACE::dextents<std::size_t, Rank>>;
using MDSPAN_IMPL_STANDARD_NAMESPACE::extents;
#endif

template <class T, std::size_t Rank>
requires (not std::is_reference_v<T>)
class mdarray {
public:
    constexpr mdarray() = default;

    template <class... Extents>
    requires (... and std::is_convertible_v<Extents, std::size_t>)
    constexpr mdarray(Extents... extents)
        : _data((... * static_cast<std::size_t>(extents)), std::remove_const_t<T>{})
        , _span(_data.data(), static_cast<std::size_t>(extents)...)
    {}

    template <class IndexType, std::size_t... Extents>
    requires (sizeof...(Extents) == Rank)
    constexpr mdarray(const extents<IndexType, Extents...>& extents) {
        [&]<std::size_t... Is>(std::index_sequence<Is...>) {
            _data = std::vector<std::remove_const_t<T>>((... * extents.extent(Is)), std::remove_const_t<T>{});
            _span = mdspan<T, Rank>(_data.data(), extents);
        }(std::make_index_sequence<Rank>{});
    }

    constexpr mdarray(const mdarray& that)
        : _data(that._data)
        , _span(_data.data(), that._span.extents())
    {}

    constexpr mdarray(mdarray&& that)
        : _data(std::move(that._data))
        , _span(_data.data(), that._span.extents())
    {
        that._span = {};
    }

    constexpr mdarray& operator=(const mdarray& that) {
        _data = that._data;
        _span = mdspan<T, Rank>(_data.data(), that._span.extents());
        return *this;
    }

    constexpr mdarray& operator=(mdarray&& that) {
        _data = std::move(that._data);
        _span = mdspan<T, Rank>(_data.data(), that._span.extents());
        that._span = {};
        return *this;
    }

    constexpr operator mdspan<T, Rank>() & {
        return _span;
    }

    constexpr operator mdspan<const T, Rank>() & {
        return _span;
    }

    template <class... Indices>
    requires (... and std::is_convertible_v<Indices, std::size_t>)
    constexpr T& operator[](Indices... indices) {
#if defined(MDSPAN_USE_BRACKET_OPERATOR) and MDSPAN_USE_BRACKET_OPERATOR == 0
        return _span(indices...);
#else
        return _span[indices...];
#endif
    }

    template <class... Indices>
    requires (... and std::is_convertible_v<Indices, std::size_t>)
    constexpr const T& operator[](Indices... indices) const {
#if defined(MDSPAN_USE_BRACKET_OPERATOR) and MDSPAN_USE_BRACKET_OPERATOR == 0
        return _span(indices...);
#else
        return _span[indices...];
#endif
    }

    constexpr T& operator[](const std::array<std::size_t, Rank>& indices) {
        return _span[indices];
    }
    constexpr const T& operator[](const std::array<std::size_t, Rank>& indices) const {
        return _span[indices];
    }

    constexpr std::size_t extent(std::size_t dimension) {
        return _span.extent(dimension);
    }

private:
    std::vector<std::remove_const_t<T>> _data{};
    mdspan<T, Rank> _span{};
};

} // namespace pstack::util

#endif // PSTACK_UTIL_MDARRAY_HPP
