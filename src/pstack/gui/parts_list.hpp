#ifndef PSTACK_GUI_PARTS_LIST_HPP
#define PSTACK_GUI_PARTS_LIST_HPP

#include "pstack/calc/part_properties.hpp"
#include "pstack/gui/list_view.hpp"
#include <wx/stattext.h>
#include <optional>
#include <string_view>

namespace pstack::gui {

using pstack::calc::part_properties;

class parts_list {
public:
    parts_list() = default;
    void initialize(wxWindow* parent, wxSize min_size);

    // Non-copyable and non-movable, because of the bound callback
    parts_list(const parts_list&) = delete;
    parts_list& operator=(const parts_list&) = delete;

    std::size_t rows() {
        return _list.rows();
    }
    void append(std::string mesh_file);
    void append(part_properties properties);
    void change(std::string mesh_file, std::size_t row);
    void reload_file(std::size_t row);
    void reload_text(std::size_t row);
    void reload_quantity(std::size_t row);
    void delete_all();
    void delete_selected();
    void get_selected(std::vector<std::size_t>& vec);

    template <class F>
    requires (std::is_invocable_r_v<void, F, const std::vector<std::size_t>&>)
    void bind(F on_select_parts) {
        const auto callback = [=](wxListEvent& event) {
            _selected.at(event.GetIndex()) = (event.GetEventType() == wxEVT_LIST_ITEM_SELECTED);
            static thread_local std::vector<std::size_t> selected{};
            get_selected(selected);
            on_select_parts(selected);
        };
        _list.control()->Bind(wxEVT_LIST_ITEM_SELECTED, callback);
        _list.control()->Bind(wxEVT_LIST_ITEM_DESELECTED, callback);
    }

    part_properties& at(std::size_t row) {
        return _properties.at(row);
    }
    std::vector<const part_properties*> get_all() {
        std::vector<const part_properties*> out{};
        out.reserve(_properties.size());
        for (auto& part : _properties) {
            out.push_back(&part);
        }
        return out;
    }

    void update_label();
    wxWindow* label() const {
        return _label;
    }
    std::size_t total_parts() const {
        return _total_parts;
    }
    double total_volume() const {
        return _total_volume;
    }
    std::size_t total_triangles() const {
        return _total_triangles;
    }

    wxWindow* control() {
        return _list.control();
    }

private:
    list_view _list{};
    std::vector<part_properties> _properties{};
    std::vector<bool> _selected{};

    wxStaticText* _label{};
    std::size_t _total_parts{};
    double _total_volume{};
    std::size_t _total_triangles{};
};

} // namespace pstack::gui

#endif // PSTACK_GUI_PARTS_LIST_HPP
