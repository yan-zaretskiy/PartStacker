#include "pstack/gui/list_view.hpp"
#include "pstack/gui/main_window.hpp"
#include <ranges>

namespace pstack::gui {

list_view::list_view(main_window* parent, const wxSize min_size, const std::vector<std::pair<wxString, int>>& columns) {
    _list = new wxListView(parent);
    _list->SetMinSize(parent->FromDIP(min_size));
    for (const auto& [label, width] : columns) {
        _list->AppendColumn(label, wxLIST_FORMAT_LEFT, parent->FromDIP(width));
    }
    _columns = columns.size();
}

list_view::row& list_view::append_row(const std::vector<wxString> items) {
    if (items.size() != _columns) {
        throw std::runtime_error("Wrong number of items in the parts list.");
    }

    const long row_index = _list->InsertItem(_rows.size(), items.at(0));
    auto& r = _rows.emplace_back(row_index);
    for (const auto& [column, item] : items | std::views::enumerate | std::views::drop(1)) {
        _list->SetItem(row_index, column, item);
    }
    return r;
}

void list_view::set_text(std::size_t row_index, int column, const wxString& text) {
    _list->SetItem(row_index, column, text);
}

} // namespace pstack::gui
