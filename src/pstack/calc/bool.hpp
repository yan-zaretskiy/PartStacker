#ifndef PSTACK_CALC_BOOL_HPP
#define PSTACK_CALC_BOOL_HPP

namespace pstack::calc {

// To be used inside a `std::vector`
class Bool {
private:
    bool value;
public:
    constexpr Bool() : value() {}
    constexpr Bool(bool b) : value(b) {}
    constexpr operator bool&() & {
        return value;
    }
    constexpr operator bool() const {
        return value;
    }
};

} // namespace pstack::calc

#endif // PSTACK_CALC_BOOL_HPP
