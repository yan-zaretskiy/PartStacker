#ifndef PSTACK_GRAPHICS_BUFFERS_HPP
#define PSTACK_GRAPHICS_BUFFERS_HPP

#include "pstack/geo/vector3.hpp"
#include <vector>

namespace pstack::graphics {

class vertex_buffer {
public:
    vertex_buffer() = default;
    vertex_buffer(const vertex_buffer&) = delete;
    vertex_buffer& operator=(const vertex_buffer&) = delete;
    vertex_buffer(vertex_buffer&&) = default;
    vertex_buffer& operator=(vertex_buffer&&) = default;

    void initialize(unsigned int location);
    void set(std::vector<geo::vector3<float>>&& vertices);

    std::size_t size() const {
        return _vertices.size();
    }

private:
    std::vector<geo::vector3<float>> _vertices{};
    unsigned int _handle = -1;
    unsigned int _location = -1;
};

class vertex_array_object {
public:
    vertex_array_object() = default;
    vertex_array_object(const vertex_array_object&) = delete;
    vertex_array_object& operator=(const vertex_array_object&) = delete;

    void initialize();
    void add_vertex_buffer(unsigned int location, std::vector<geo::vector3<float>>&& vertices);

    void bind_arrays();

    const auto& operator[](std::size_t i) const {
        return _vertex_buffers[i];
    }

    void clear() {
        _vertex_buffers.clear();
    }

private:
    std::vector<vertex_buffer> _vertex_buffers{};
    unsigned int _handle = -1;
};

} // namespace pstack::graphics

#endif // PSTACK_GRAPHICS_BUFFERS_HPP
