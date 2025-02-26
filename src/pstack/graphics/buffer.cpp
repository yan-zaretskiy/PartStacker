#include <GL/glew.h> // must be included first
#include "pstack/graphics/buffer.hpp"

#include <cassert>

namespace pstack::graphics {

void vertex_buffer::initialize(GLuint location) {
    static_assert(std::same_as<GLuint, decltype(_handle)>);
    assert(-1 == _handle);
    assert(-1 == _location);
    assert(_vertices.empty());
    glGenBuffers(1, &_handle);
    _location = location;
}

void vertex_buffer::set(std::vector<geo::vector3<float>>&& vertices) {
    _vertices = std::move(vertices);
    glBindBuffer(GL_ARRAY_BUFFER, _handle);
    glBufferData(GL_ARRAY_BUFFER, _vertices.size() * sizeof(_vertices[0]), _vertices.data(), GL_STATIC_DRAW);
    glVertexAttribPointer(_location, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(_location);
    glBindBuffer(GL_ARRAY_BUFFER, 0);
}

void vertex_array_object::initialize() {
    static_assert(std::same_as<GLuint, decltype(_handle)>);
    assert(-1 == _handle);
    assert(_vertex_buffers.empty());
    glGenVertexArrays(1, &_handle);
}

void vertex_array_object::add_vertex_buffer(GLuint location, std::vector<geo::vector3<float>>&& vertices) {
    glBindVertexArray(_handle);
    auto& buffer = _vertex_buffers.emplace_back();
    buffer.initialize(location);
    buffer.set(std::move(vertices));
    glBindVertexArray(0);
}

void vertex_array_object::bind_arrays() {
    glBindVertexArray(_handle);
}

} // namespace pstack::graphics
