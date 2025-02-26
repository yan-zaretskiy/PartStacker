#ifndef PSTACK_GUI_PARTS_LIST_HPP
#define PSTACK_GUI_PARTS_LIST_HPP

#include "pstack/gui/list_view.hpp"
#include "pstack/gui/part_properties.hpp"
#include <wx/stattext.h>
#include <optional>
#include <string_view>

namespace pstack::gui {

class parts_list {
public:
    parts_list() = default;
    parts_list(main_window* parent, wxSize min_size, void(main_window::*set_part)(std::optional<std::size_t>));

    // Non-copyable and non-movable, because of the bound callback
    parts_list(const parts_list&) = delete;
    parts_list& operator=(const parts_list&) = delete;

    void append_row(std::string mesh_file);
    void refresh_quantity_text();
    void delete_selected();
    
    void update_label();
    wxWindow* label() const {
        return _label;
    }
    
    list_view& list() {
        return _list;
    }

private:
    list_view _list{};
    wxStaticText* _label{};
    std::vector<bool> _selected{};
};

} // namespace pstack::gui

#endif PSTACK_GUI_PARTS_LIST_HPP
