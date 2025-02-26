#include "pstack/graphics/global.hpp" // OpenGL utilities must be included first

#include "pstack/gui/main_window.hpp"
#include "pstack/gui/viewport.hpp"
#include <wx/dcclient.h>
#include <wx/msgdlg.h>

#include <format>
#include <iostream>

namespace pstack::gui {

viewport::viewport(main_window* parent, const wxGLAttributes& canvasAttrs)
    : wxGLCanvas(parent, canvasAttrs)
{
    wxGLContextAttrs ctx_attrs;
    ctx_attrs.PlatformDefaults().CoreProfile().OGLVersion(3, 3).EndList();
    _opengl_context = std::make_unique<wxGLContext>(this, nullptr, &ctx_attrs);

    if (!_opengl_context->IsOK()) {
        wxMessageBox("PartStacker requires an OpenGL 3.3 capable driver.",
                     "OpenGL version error", wxOK | wxICON_INFORMATION, this);
        _opengl_context.reset();
    }

    Bind(wxEVT_PAINT, &viewport::on_paint, this);
    Bind(wxEVT_SIZE, &viewport::on_size, this);
    
	Bind(wxEVT_LEFT_DOWN, &viewport::on_left_down, this);
    Bind(wxEVT_MOUSEWHEEL, &viewport::on_scroll, this);
}

constexpr auto vertex_shader_source = R"(
    #version 330 core
    layout (location = 0) in vec3 aPos;
    layout (location = 1) in vec3 aNormal;
    out vec4 fragNormal;
    uniform mat4 transform_vertices;
    uniform mat4 transform_normals;
    void main() {
        gl_Position = transform_vertices * vec4(aPos, 1.0);
        fragNormal = transform_normals * vec4(aNormal, 1.0);
    }
)";

constexpr auto fragment_shader_source = R"(
    #version 330 core
    in vec4 fragNormal;
    out vec4 fragColour;
    void main() {
        float shade = (-fragNormal.z / 4) + 0.75;
        fragColour = vec4(shade, shade, shade, 1.0);
    }
)";

namespace {

geo::mesh default_tetrahedron_mesh() {
    static constexpr float sqrt3 = 0.57735026919f;

    using point3 = geo::point3<float>;
    using vector3 = geo::vector3<float>;

    std::vector<geo::triangle> triangles{};
    triangles.emplace_back(
        vector3{sqrt3, sqrt3, -sqrt3},
        point3{0.5f, 0.5f, 0.5f},
        point3{0.5f, -0.5f, -0.5f},
        point3{-0.5f, 0.5f, -0.5f}
    );
    triangles.emplace_back(
        vector3{sqrt3, -sqrt3, sqrt3},
        point3{0.5f, 0.5f, 0.5f},
        point3{-0.5f, -0.5f, 0.5f},
        point3{0.5f, -0.5f, -0.5f}
    );
    triangles.emplace_back(
        vector3{-sqrt3, sqrt3, sqrt3},
        point3{0.5f, 0.5f, 0.5f},
        point3{-0.5f, 0.5f, -0.5f},
        point3{-0.5f, -0.5f, 0.5f}
    );
    triangles.emplace_back(
        vector3{-sqrt3, -sqrt3, -sqrt3},
        point3{0.5f, -0.5f, -0.5f},
        point3{-0.5f, -0.5f, 0.5f},
        point3{-0.5f, 0.5f, -0.5f}
    );

    return geo::mesh(std::move(triangles));
}

} // namespace

bool viewport::initialize() {
    if (!_opengl_context) {
        return false;
    }

    SetCurrent(*_opengl_context);

    if (const auto err = graphics::initialize();
        not err.has_value())
    {
        wxMessageBox(std::format("OpenGL GLEW initialization failed with message: \"{}\"", err.error()),
                     "OpenGL initialization error", wxOK | wxICON_ERROR, this);
        return false;
    }

    if (const auto err = _shader.initialize(vertex_shader_source, fragment_shader_source);
        not err.has_value())
    {
        wxMessageBox(std::format("Error in creating OpenGL shader.\n{}", err.error()),
                     "OpenGL shader error", wxOK | wxICON_ERROR, this);
        return false;
    }

    _vao.initialize();
    const auto mesh = default_tetrahedron_mesh();
    const auto centroid = mesh.volume_and_centroid().centroid;
    set_mesh(mesh, centroid);

    return true;
}

void viewport::on_paint(wxPaintEvent&) {
    wxPaintDC dc(this);
	render(dc);
}

void viewport::render() {
	wxClientDC dc(this);
	render(dc);
}

void viewport::render(wxDC& dc) {
    if (!_opengl_initialized) {
        return;
    }

    SetCurrent(*_opengl_context);

    graphics::clear(40 / 255.0, 50 / 255.0, 120 / 255.0, 1);
    _shader.use_program();
    _vao.bind_arrays();
    graphics::draw_triangles(_vao[0].size());

    SwapBuffers();
}

void viewport::on_size(wxSizeEvent& event) {
    const bool first_appearance = IsShownOnScreen() && !_opengl_initialized;
    if (first_appearance) {
        _opengl_initialized = initialize();
    }

    if (_opengl_initialized) {
        _viewport_size = event.GetSize() * GetContentScaleFactor();
        graphics::viewport(0, 0, _viewport_size.x, _viewport_size.y);

        static constexpr float scale_baseline = 866;
        _transform.scale_screen(scale_baseline / _viewport_size.x, scale_baseline / _viewport_size.y);
        _shader.set_uniform("transform_vertices", _transform.for_vertices());
        _shader.set_uniform("transform_normals", _transform.for_normals());

        if (first_appearance) {
            render();
        }
    }

    event.Skip();
}

void viewport::set_mesh(const geo::mesh& mesh, const geo::point3<float>& centroid) {
    _vao.clear();

    using vector3 = geo::vector3<float>;
    
    std::vector<vector3> vertices;
    std::vector<vector3> normals;
    for (const auto& t : mesh.triangles()) {
        vertices.push_back(t.v1.as_vector());
        vertices.push_back(t.v2.as_vector());
        vertices.push_back(t.v3.as_vector());
        normals.push_back(t.normal);
        normals.push_back(t.normal);
        normals.push_back(t.normal);
    }

    _vao.add_vertex_buffer(0, std::move(vertices));
    _vao.add_vertex_buffer(1, std::move(normals));

    const auto bounding = mesh.bounding();
    _transform.translation(geo::origin3<float> - centroid);
    const auto size = bounding.max - bounding.min;
    const auto zoom_factor = 1 / std::max({ size.x, size.y, size.z });
    _transform.scale_mesh(zoom_factor);
    _shader.set_uniform("transform_vertices", _transform.for_vertices());
    _shader.set_uniform("transform_normals", _transform.for_normals());
}

void viewport::on_left_down(wxMouseEvent& evt) {
    starting_pos = evt.GetPosition();
	Bind(wxEVT_LEFT_UP, &viewport::on_left_up, this);
	Bind(wxEVT_MOTION, &viewport::on_motion, this);
	Bind(wxEVT_MOUSE_CAPTURE_LOST, &viewport::on_capture_lost, this);
	CaptureMouse();
}

void viewport::on_motion(wxMouseEvent& evt) {
    on_move_by(evt.GetPosition());
}

void viewport::on_left_up(wxMouseEvent& evt) {
    on_move_by(evt.GetPosition());
    on_finish_move();
}

void viewport::on_capture_lost(wxMouseCaptureLostEvent& evt) {
    on_finish_move();
}

void viewport::on_scroll(wxMouseEvent& evt) {
    const float zoom_factor = (float)std::pow(2.0, ((double)evt.GetWheelRotation() / (double)evt.GetWheelDelta()) / 4);    
    _transform.zoom_by(zoom_factor);
    _shader.set_uniform("transform_vertices", _transform.for_vertices());
    _shader.set_uniform("transform_normals", _transform.for_normals());
    render();
}

void viewport::on_move_by(wxPoint position) {
    const auto [dx, dy] = starting_pos - position;
    starting_pos = position;
    _transform.rotate_by((float)dy / 256, (float)dx / 256);
    _shader.set_uniform("transform_vertices", _transform.for_vertices());
    _shader.set_uniform("transform_normals", _transform.for_normals());
    render();
}

void viewport::on_finish_move() {
	if (HasCapture()) {
		ReleaseMouse();
	}
	Unbind(wxEVT_LEFT_UP, &viewport::on_left_up, this);
	Unbind(wxEVT_MOTION, &viewport::on_motion, this);
	Unbind(wxEVT_MOUSE_CAPTURE_LOST, &viewport::on_capture_lost, this);
}

} // namespace pstack::gui
