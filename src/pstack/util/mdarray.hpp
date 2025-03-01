#ifndef PSTACK_UTIL_MDARRAY_HPP
#define PSTACK_UTIL_MDARRAY_HPP

#include <mdspan>
#include <type_traits>
#include <vector>

namespace pstack::util {

template <class T, std::size_t Rank>
requires (not std::is_reference_v<T>)
class mdarray {
public:
    template <std::convertible_to<std::size_t>... Extents>
    constexpr mdarray(Extents... extents)
        : _data((... * static_cast<std::size_t>(extents)), std::remove_const_t<T>{})
        , _span(_data.data(), static_cast<std::size_t>(extents)...)
    {}
    
    template <class IndexType, std::size_t... Extents>
    requires (sizeof...(Extents) == Rank)
    constexpr mdarray(const std::extents<IndexType, Extents...>& extents) {
        [&]<std::size_t... Is>(std::index_sequence<Is...>) {
            _data = std::vector<std::remove_const_t<T>>((... * extents.extent(Is)), std::remove_const_t<T>{});
            _span = std::mdspan<T, std::dextents<std::size_t, Rank>>(_data.data(), extents);
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
        _span = std::mdspan<T, std::dextents<std::size_t, Rank>>(_data.data(), that._span.extents());
    }

    constexpr mdarray& operator=(mdarray&& that) {
        _data = std::move(that._data);
        _span = std::mdspan<T, std::dextents<std::size_t, Rank>>(_data.data(), that._span.extents());
        that._span = {};
    }

    constexpr operator std::mdspan<T, std::dextents<std::size_t, Rank>>() & {
        return _span;
    }

    constexpr operator std::mdspan<const T, std::dextents<std::size_t, Rank>>() & {
        return _span;
    }

    template <std::convertible_to<std::size_t>... Indices>
    constexpr T& operator[](Indices... indices) {
        return _span[indices...];
    }

    template <std::convertible_to<std::size_t>... Indices>
    constexpr const T& operator[](Indices... indices) const {
        return _span[indices...];
    }

private:
    std::vector<std::remove_const_t<T>> _data;
    std::mdspan<T, std::dextents<std::size_t, Rank>> _span;
};

} // namespace pstack::util

#endif // PSTACK_UTIL_MDARRAY_HPP
