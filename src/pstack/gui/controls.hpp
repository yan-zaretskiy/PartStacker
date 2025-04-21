#ifndef PSTACK_GUI_CONTROLS_HPP
#define PSTACK_GUI_CONTROLS_HPP

#include <wx/button.h>
#include <wx/checkbox.h>
#include <wx/gauge.h>
#include <wx/notebook.h>
#include <wx/panel.h>
#include <wx/radiobut.h>
#include <wx/spinctrl.h>
#include <vector>

namespace pstack::gui {

class main_window;

struct controls {
    controls() = default;
    controls(const controls&) = delete;
    controls& operator=(const controls&) = delete;
    void initialize(main_window* parent);
    void reset_values();

    wxButton* import_button;
    wxButton* delete_button;
    wxButton* change_button;
    wxButton* reload_button;
    
    wxSpinCtrlDouble* min_clearance_spinner;
    wxCheckBox* section_view_checkbox;
    wxButton* export_button;
    wxButton* stack_button;
    wxGauge* progress_bar;

    wxNotebook* notebook;
    std::vector<wxPanel*> notebook_panels;
    
    // Part settings tab
    wxSpinCtrl* quantity_spinner;
    wxSpinCtrl* min_hole_spinner;
    wxCheckBox* minimize_checkbox;
    wxRadioButton* radio_none;
    wxRadioButton* radio_arbitrary;
    wxRadioButton* radio_cubic;
    wxButton* preview_button;
    wxButton* copy_button;
    wxButton* mirror_button;
    
    // Sinterbox tab
    wxSpinCtrlDouble* clearance_spinner;
    wxSpinCtrlDouble* spacing_spinner;
    wxSpinCtrlDouble* thickness_spinner;
    wxSpinCtrlDouble* width_spinner;
    wxCheckBox* sinterbox_checkbox;

    // Bounding box tab
    wxSpinCtrl* initial_x_spinner;
    wxSpinCtrl* initial_y_spinner;
    wxSpinCtrl* initial_z_spinner;
    wxSpinCtrl* maximum_x_spinner;
    wxSpinCtrl* maximum_y_spinner;
    wxSpinCtrl* maximum_z_spinner;
};

} // namespace pstack::gui

#endif // PSTACK_GUI_CONTROLS_HPP
