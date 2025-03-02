#ifndef PSTACK_CALC_STACKER_HPP
#define PSTACK_CALC_STACKER_HPP

#include "pstack/calc/part_properties.hpp"
#include <atomic>
#include <functional>
#include <vector>

namespace pstack::calc {

struct stacker_parameters {
    std::vector<const part_properties*> parts;

    std::function<void(double, double)> set_progress;
    std::function<void(const mesh&, int, int, int)> display_mesh;
    std::function<void(mesh)> on_success;
    std::function<void()> on_failure;
    std::function<void()> on_finish;
    
    double resolution;
    int x_min, x_max;
    int y_min, y_max;
    int z_min, z_max;
};

class stacker {
public:
    stacker()
        : _running(false)
    {}

    stacker(const stacker&) = delete;
    stacker& operator=(const stacker&) = delete;

    bool running() const {
        return _running;
    }

    void stack(stacker_parameters params);

    void abort() {
        _running = false;
    }

private:
    std::atomic<bool> _running;
};

} // namespace pstack::calc

#endif // PSTACK_CALC_STACKER_HPP
