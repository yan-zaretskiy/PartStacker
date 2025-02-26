#ifndef PSTACK_GUI_LIST_VIEW_HPP
#define PSTACK_GUI_LIST_VIEW_HPP

#include <wx/listctrl.h>
#include <wx/string.h>
#include <any>
#include <utility>
#include <vector>
#include "pstack/geo/mesh.hpp"

namespace pstack::gui {

class main_window;

class list_view {
public:
    list_view() = default;
    list_view(main_window* parent, wxSize min_size, const std::vector<std::pair<wxString, int>>& columns);

    class row;
    row& append_row(std::vector<wxString> items);
    void set_text(std::size_t row_index, int column, const wxString& text);
    std::size_t rows() const {
        return _rows.size();
    }
    row& row_at(std::size_t i) {
        return _rows.at(i);
    }

    wxWindow* control() const {
        return _list;
    }

    template <class... Args>
    void bind(Args&&... args) {
        _list->Bind(std::forward<Args>(args)...);
    }

private:
    wxListView* _list = nullptr;
    std::vector<row> _rows;
    std::size_t _columns = 0;
};

class list_view::row {
public:
    row(long index)
        : _index(index)
    {}

    long index() const {
        return _index;
    }

    template <class T>
    void set_data(T&& t) {
        _data.emplace<T>(std::forward<T>(t));
    }

    template <class T>
    T& get_data() {
        return *std::any_cast<T>(&_data);
    }

private:
    long _index;
    std::any _data{};
};

} // namespace pstack::gui

#endif PSTACK_GUI_LIST_VIEW_HPP
