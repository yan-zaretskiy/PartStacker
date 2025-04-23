#ifndef PSTACK_GUI_LIST_VIEW_HPP
#define PSTACK_GUI_LIST_VIEW_HPP

#include "pstack/calc/mesh.hpp"
#include <wx/listctrl.h>
#include <wx/string.h>
#include <utility>
#include <vector>

namespace pstack::gui {

class list_view {
public:
    list_view() = default;
    std::size_t rows() const {
        return _rows;
    }
    wxWindow* control() const {
        return _list;
    }

    void get_selected(std::vector<std::size_t>& vec);

    template <class F>
    requires (std::is_invocable_r_v<void, F, const std::vector<std::size_t>&>)
    void bind(F on_selected_items) {
        const auto callback = [=](wxListEvent& event) {
            _selected.at(event.GetIndex()) = (event.GetEventType() == wxEVT_LIST_ITEM_SELECTED);
            static thread_local std::vector<std::size_t> selected{};
            get_selected(selected);
            on_selected_items(selected);
        };
        _list->Bind(wxEVT_LIST_ITEM_SELECTED, callback);
        _list->Bind(wxEVT_LIST_ITEM_DESELECTED, callback);
    }

protected:
    void initialize(wxWindow* parent, const std::vector<std::pair<wxString, int>>& columns);
    void append(std::vector<wxString> items);
    void replace(std::size_t row_index, std::vector<wxString> items);
    void delete_row(std::size_t row_index);
    void delete_all();
    void set_text(std::size_t row_index, int column, const wxString& text);

    void deselect(std::size_t row_index) {
        _selected.at(row_index) = false;
    }

    template <class T>
    void delete_selected(std::vector<T>& data) {
        static thread_local std::vector<std::size_t> indices_to_delete{};
        get_selected(indices_to_delete);
        for (auto it = indices_to_delete.rbegin(); it != indices_to_delete.rend(); ++it) {
            const std::size_t index = *it;
            delete_row(index);
            data.erase(data.begin() + index);
            _selected.erase(_selected.begin() + index);
        }
    }

private:
    wxListView* _list = nullptr;
    std::size_t _rows = 0;
    std::size_t _columns = 0;
    std::vector<bool> _selected{};
};

} // namespace pstack::gui

#endif // PSTACK_GUI_LIST_VIEW_HPP
