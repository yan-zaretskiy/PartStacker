#ifndef PSTACK_CALC_STACKER_THREAD_HPP
#define PSTACK_CALC_STACKER_THREAD_HPP

#include "pstack/calc/stacker.hpp"
#include <atomic>
#include <optional>
#include <stdexcept>
#include <thread>

namespace pstack::calc {

class stacker_thread {
public:
    stacker_thread() = default;
    ~stacker_thread() {
        stop();
    }

    void start(stacker_parameters params) {
        if (_thread.has_value()) {
            throw std::runtime_error("Thread already exists");
        }
        _thread.emplace([this, params = std::move(params)] {
            _stacker.stack(std::move(params));
            _thread->detach();
            _thread.reset();
        });
    }
    
    void stop() {
        _stacker.stop();
        if (_thread.has_value()) {
            _thread->join();
        }
        _thread.reset();
    }

    bool running() const {
        return _stacker.running();
    }

private:
    stacker _stacker{};
    std::optional<std::thread> _thread{};
};

} // namespace pstack::calc

#endif // PSTACK_CALC_STACKER_THREAD_HPP
