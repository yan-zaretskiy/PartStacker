#include "pstack/files/stl.hpp"
#include "pstack/gui/main_window.hpp"
#include "pstack/gui/parts_list.hpp"
#include <cmath>
#include <filesystem>
#include <format>
#include <ranges>

namespace pstack::gui {

namespace {

part_properties make_properties(std::string mesh_file) {
    part_properties properties;
    properties.mesh_file = std::move(mesh_file);
    properties.name = std::filesystem::path(properties.mesh_file).stem().string();
    properties.mesh = files::from_stl(properties.mesh_file);

    properties.quantity = [name = properties.name]() mutable -> int {
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
    
    auto volume_and_centroid = properties.mesh.volume_and_centroid();
    properties.volume = volume_and_centroid.volume;
    properties.centroid = volume_and_centroid.centroid;
    properties.triangle_count = properties.mesh.triangles().size();
    properties.mirrored = false;
    properties.min_hole = 1;
    properties.rotation_index = 1;
    properties.rotate_min_box = false;
    return properties;
}

} // namespace

parts_list::parts_list(main_window* parent, wxSize min_size, void(main_window::*set_part)(std::optional<std::size_t>)) {
    _list = list_view(parent, min_size, {
        { "Name", 105 },
        { "Quantity", 60 },
        { "Volume", 60 },
        { "Triangles", 60 },
        { "Comment", 90 },
    });
    _label = new wxStaticText(parent, wxID_ANY, "");

    _list.bind(wxEVT_LIST_ITEM_SELECTED, [this, parent, set_part](wxListEvent& event) {
        _selected.at(event.GetIndex()) = true;
        if (std::ranges::count(_selected, true) == 1) {
            (parent->*set_part)(event.GetIndex());
        } else {
            (parent->*set_part)(std::nullopt);
        }
    });
    _list.bind(wxEVT_LIST_ITEM_DESELECTED, [this, parent, set_part](wxListEvent& event) {
        _selected.at(event.GetIndex()) = false;
        if (std::ranges::count(_selected, true) == 1) {
            (parent->*set_part)(std::ranges::find(_selected, true) - _selected.begin());
        } else {
            (parent->*set_part)(std::nullopt);
        }
    });
}

void parts_list::append_row(std::string mesh_file) {
    part_properties properties = make_properties(std::move(mesh_file));
    _list.append_row({
        properties.name,
        std::to_string(properties.quantity),
        std::format("{:.2f}", properties.volume / 1000),
        std::to_string(properties.triangle_count),
        (properties.mirrored ? "Mirrored" : "")
    });
    _properties.push_back(std::move(properties));
    _selected.push_back(false);
}

void parts_list::refresh_quantity_text() {
    for (std::size_t row : std::views::iota(0ull, _list.rows())) {
        _list.set_text(row, 1, std::to_string(_properties[row].quantity));
    }
    update_label();
}

void parts_list::delete_selected() {
    std::vector<std::size_t> indices_to_delete{};
    for (const auto [index, value] : _selected | std::views::enumerate) {
        if (value) {
            indices_to_delete.push_back(index);
        }
    }
    for (const std::size_t index : indices_to_delete | std::views::reverse) {
        _list.delete_row(index);
        _properties.erase(_properties.begin() + index);
        _selected.erase(_selected.begin() + index);
    }
}

void parts_list::update_label() {
    int parts = 0;
    double volume = 0;
    int triangles = 0;
    for (const std::size_t row : std::views::iota(0ull, _list.rows())) {
        const auto& properties = _properties.at(row);
        parts += properties.quantity;
        volume += properties.quantity * properties.volume;
        triangles += properties.quantity * properties.triangle_count;
    }
    _label->SetLabelText(std::format("Parts: {} - Volume: {:.1f} - Triangles: {}", parts, volume / 1000, triangles));
}

} // namespace pstack::gui
