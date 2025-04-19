#include "pstack/gui/constants.hpp"
#include "pstack/gui/list_view.hpp"

namespace pstack::gui {

void list_view::get_selected(std::vector<std::size_t>& vec) {
    vec.clear();
    std::size_t index = 0;
    for (const bool value : _selected) {
        if (value) {
            vec.push_back(index);
        }
        ++index;
    }
}

void list_view::initialize(wxWindow* parent, const std::vector<std::pair<wxString, int>>& columns) {
    _list = new wxListView(parent);
    _list->SetMinSize(parent->FromDIP(constants::min_list_size));
    for (const auto& [label, width] : columns) {
        _list->AppendColumn(label, wxLIST_FORMAT_LEFT, parent->FromDIP(width));
    }
    _columns = columns.size();
}

void list_view::append(const std::vector<wxString> items) {
    if (items.size() != _columns) {
        throw std::runtime_error("Wrong number of items in the parts list.");
    }

    const auto row_index = _rows++;
    _list->InsertItem(row_index, items.at(0));
    std::size_t column = 1;
    for (auto it = items.begin() + 1; it != items.end(); ++it) {
        _list->SetItem(row_index, column, *it);
        ++column;
    }
    _selected.push_back(false);
}

void list_view::replace(const std::size_t row_index, const std::vector<wxString> items) {
    if (items.size() != _columns) {
        throw std::runtime_error("Wrong number of items in the parts list.");
    }
    if (row_index >= _rows) {
        throw std::runtime_error("Parts list row index out of bounds.");
    }

    std::size_t column = 0;
    for (const wxString& item : items) {
        _list->SetItem(row_index, column, item);
        ++column;
    }
}

void list_view::delete_row(const std::size_t row_index) {
    if (row_index >= _rows) {
        throw std::runtime_error("Parts list row index out of bounds.");
    }
    _list->DeleteItem(row_index);
    --_rows;
}

void list_view::delete_all() {
    while (_rows != 0) {
        _list->DeleteItem(--_rows);
    }
    _selected.clear();
}

void list_view::set_text(std::size_t row_index, int column, const wxString& text) {
    _list->SetItem(row_index, column, text);
}

} // namespace pstack::gui
