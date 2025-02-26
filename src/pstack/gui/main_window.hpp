#ifndef PSTACK_GUI_MAIN_WINDOW_HPP
#define PSTACK_GUI_MAIN_WINDOW_HPP

#include <wx/checkbox.h>
#include <wx/frame.h>
#include <wx/radiobut.h>
#include <wx/sizer.h>
#include <wx/spinctrl.h>
#include <wx/string.h>
#include <optional>
#include <vector>
#include "pstack/gui/parts_list.hpp"
#include "pstack/gui/part_properties.hpp"

namespace pstack::gui {

class viewport;

class main_window : public wxFrame {
public:
    main_window(const wxString& title);
    void after_show();

private:
    viewport* _viewport = nullptr;

    void set_part(std::optional<std::size_t> index);
    parts_list _parts_list{ this, wxSize(380, 240), &main_window::set_part };
    part_properties* _current_part = nullptr;
    void enable_part_settings(bool enable);
    wxSpinCtrl* _quantity_spinner = nullptr;
    wxSpinCtrl* _min_hole_spinner = nullptr;
    wxCheckBox* _minimize_checkbox = nullptr;
    wxRadioButton* _radio_none = nullptr;
    wxRadioButton* _radio_arbitrary = nullptr;
    wxRadioButton* _radio_cubic = nullptr;

    wxButton* _import_button = nullptr;
    wxButton* _delete_button = nullptr;
    wxButton* _change_button = nullptr;
    wxButton* _reload_button = nullptr;

    static wxMenuBar* make_menu_bar();

    static wxSizer* make_part_buttons(main_window* frame);
    static wxSizer* make_bottom_section1(main_window* frame);
    static wxSizer* make_bottom_section2(main_window* frame);

    static wxWindow* make_tabs(main_window* frame);

    void on_import(wxCommandEvent& event);
    void on_delete(wxCommandEvent& event);
    void on_change(wxCommandEvent& event);
    void on_reload(wxCommandEvent& event);
};

} // namespace pstack::gui

#endif // PSTACK_GUI_MAIN_WINDOW_HPP
