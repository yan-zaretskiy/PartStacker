#include "pstack/files/stl.hpp"
#include "pstack/gui/parts_list.hpp"
#include <wx/string.h>
#include <charconv>
#include <cmath>
#include <filesystem>
#include <ranges>

namespace pstack::gui {

namespace {

calc::part make_part(std::string mesh_file, bool mirrored) {
    calc::part part;
    part.mesh_file = std::move(mesh_file);
    part.name = std::filesystem::path(part.mesh_file).stem().string();
    part.mesh = files::from_stl(part.mesh_file);
    part.mesh.set_baseline({ 0, 0, 0 });

    part.quantity = [name = part.name]() mutable -> int {
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
            return 1; // Default is 1, if we can't parse a number
        }
        std::string_view number{ name.data() + (name.size() - number_length), name.data() + name.size() };
        int out{-1};
        std::from_chars(number.data(), number.data() + number.size(), out);
        return out;
    }();

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

} // namespace

void parts_list::initialize(wxWindow* parent) {
    _list = list_view(parent, {
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
    _list.append({
        part.name,
        std::to_string(part.quantity),
        wxString::Format("%.2f", part.volume / 1000),
        std::to_string(part.triangle_count),
        (part.mirrored ? "Mirrored" : "")
    });
    _parts.push_back(std::make_shared<calc::part>(std::move(part)));
    _selected.push_back(false);
}

void parts_list::change(std::string mesh_file, const std::size_t row) {
    calc::part& part = *_parts.at(row);
    part = make_part(std::move(mesh_file), part.mirrored);
    reload_text(row);
    _selected.at(row) = false;
}

void parts_list::reload_file(const std::size_t row) {
    change(std::move(_parts.at(row)->mesh_file), row);
}

void parts_list::reload_text(const std::size_t row) {
    const calc::part& part = *_parts.at(row);
    _list.replace(row, {
        part.name,
        std::to_string(part.quantity),
        wxString::Format("%.2f", part.volume / 1000),
        std::to_string(part.triangle_count),
        (part.mirrored ? "Mirrored" : "")
    });
}

void parts_list::reload_quantity(std::size_t row) {
    _list.set_text(row, 1, std::to_string(_parts.at(row)->quantity));
    update_label();
}

void parts_list::delete_all() {
    for (std::size_t index = _parts.size(); index-- != 0; ) {
        _list.delete_row(index);
    }
    _parts.clear();
    _selected.clear();
    update_label();
}

void parts_list::delete_selected() {
    static thread_local std::vector<std::size_t> indices_to_delete{};
    get_selected(indices_to_delete);
    for (const std::size_t index : indices_to_delete | std::views::reverse) {
        _list.delete_row(index);
        _parts.erase(_parts.begin() + index);
        _selected.erase(_selected.begin() + index);
    }
}

void parts_list::get_selected(std::vector<std::size_t>& vec) {
    vec.clear();
    std::size_t index = 0;
    for (const bool value : _selected) {
        if (value) {
            vec.push_back(index);
        }
        ++index;
    }
}

std::vector<std::shared_ptr<const calc::part>> parts_list::get_all() const {
    std::vector<std::shared_ptr<const calc::part>> out{};
    out.reserve(_parts.size());
    std::ranges::copy(_parts, std::back_inserter(out));
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
    _label->SetLabelText(wxString::Format("Parts: %zu - Volume: %.1f - Triangles: %zu", _total_parts, _total_volume / 1000, _total_triangles));
}

} // namespace pstack::gui
