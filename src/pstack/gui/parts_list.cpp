#include "pstack/files/stl.hpp"
#include "pstack/gui/parts_list.hpp"
#include <wx/string.h>
#include <charconv>
#include <cmath>
#include <filesystem>

namespace pstack::gui {

namespace {

calc::part make_part(std::string mesh_file, bool mirrored) {
    calc::part part;
    part.mesh_file = std::move(mesh_file);
    part.name = std::filesystem::path(part.mesh_file).stem().string();
    part.mesh = files::from_stl(part.mesh_file);
    part.mesh.set_baseline({ 0, 0, 0 });

    part.base_quantity = [name = part.name]() mutable -> std::optional<int> {
        char looking_for = '.';
        if (name.ends_with(')')) {
            name.pop_back();
            looking_for = '(';
        }
        std::size_t number_length = 0;
        while (name.size() > number_length and std::isdigit(name[name.size() - number_length - 1])) {
            ++number_length;
        }
        if (number_length == 0 or not (name.size() > number_length and name[name.size() - number_length - 1] == looking_for)) {
            return std::nullopt;
        }
        std::string_view number{ name.data() + (name.size() - number_length), number_length };
        int out{-1};
        std::from_chars(number.data(), number.data() + number.size(), out);
        return out;
    }();
    part.quantity = part.base_quantity.value_or(1);

    auto volume_and_centroid = part.mesh.volume_and_centroid();
    part.volume = volume_and_centroid.volume;
    part.centroid = volume_and_centroid.centroid;
    part.triangle_count = part.mesh.triangles().size();
    part.mirrored = mirrored;
    part.min_hole = 1;
    part.rotation_index = 1;
    part.rotate_min_box = false;
    return part;
}

wxString quantity_string(const calc::part& part, const bool show_extra) {
    if (show_extra and part.base_quantity.has_value()) {
        const int diff = part.quantity - *part.base_quantity;
        if (diff > 0) {
            return wxString::Format("%d + %d", *part.base_quantity, diff);
        }
    }
    return wxString::Format("%d", part.quantity);
}

} // namespace

void parts_list::initialize(wxWindow* parent) {
    list_view::initialize(parent, {
        { "Name", 105 },
        { "Quantity", 60 },
        { "Volume", 60 },
        { "Triangles", 60 },
        { "Comment", 90 },
    });
    _label = new wxStaticText(parent, wxID_ANY, "");
    update_label();
}

void parts_list::append(std::string mesh_file) {
    append(make_part(std::move(mesh_file), false));
}

void parts_list::append(calc::part part) {
    list_view::append({
        part.name,
        quantity_string(part, _show_extra),
        wxString::Format("%.2f", part.volume / 1000),
        std::to_string(part.triangle_count),
        (part.mirrored ? "Mirrored" : "")
    });
    _parts.push_back(std::make_shared<calc::part>(std::move(part)));
}

void parts_list::change(std::string mesh_file, const std::size_t row) {
    calc::part& part = *_parts.at(row);
    part = make_part(std::move(mesh_file), part.mirrored);
    reload_text(row);
    list_view::deselect(row);
}

void parts_list::reload_file(const std::size_t row) {
    change(std::move(_parts.at(row)->mesh_file), row);
}

void parts_list::reload_text(const std::size_t row) {
    const calc::part& part = *_parts.at(row);
    list_view::replace(row, {
        part.name,
        quantity_string(part, _show_extra),
        wxString::Format("%.2f", part.volume / 1000),
        std::to_string(part.triangle_count),
        (part.mirrored ? "Mirrored" : "")
    });
}

void parts_list::reload_all_text() {
    for (std::size_t row = 0; row != rows(); ++row) {
        reload_text(row);
    }
}

void parts_list::reload_quantity(std::size_t row) {
    list_view::set_text(row, 1, quantity_string(*_parts.at(row), _show_extra));
    update_label();
}

void parts_list::delete_all() {
    list_view::delete_all();
    _parts.clear();
    update_label();
}

void parts_list::delete_selected() {
    list_view::delete_selected(_parts);
}

std::vector<std::shared_ptr<const calc::part>> parts_list::get_all() const {
    std::vector<std::shared_ptr<const calc::part>> out{};
    out.reserve(_parts.size());
    std::copy(_parts.begin(), _parts.end(), std::back_inserter(out));
    return out;
}

void parts_list::update_label() {
    _total_parts = 0;
    _total_volume = 0;
    _total_triangles = 0;
    for (std::shared_ptr<const calc::part> part : _parts) {
        _total_parts += part->quantity;
        _total_volume += part->quantity * part->volume;
        _total_triangles += part->quantity * part->triangle_count;
    }
    _label->SetLabelText(wxString::Format("Files: %zu - Parts: %zu - Volume: %.1f - Triangles: %zu",
        _parts.size(), _total_parts, _total_volume / 1000, _total_triangles));
}

} // namespace pstack::gui
