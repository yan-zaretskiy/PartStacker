#ifndef PSTACK_GUI_CONTROLS_HPP
#define PSTACK_GUI_CONTROLS_HPP

#include <wx/button.h>
#include <wx/checkbox.h>
#include <wx/choice.h>
#include <wx/gauge.h>
#include <wx/notebook.h>
#include <wx/panel.h>
#include <wx/spinctrl.h>
#include <wx/stattext.h>
#include <vector>

namespace pstack::gui {

class main_window;

struct controls {
    controls() = default;
    controls(const controls&) = delete;
    controls& operator=(const controls&) = delete;
    void initialize(main_window* parent);
    void reset_values();

    wxButton* import_part_button;
    wxButton* delete_part_button;
    wxButton* reload_part_button;
    wxButton* copy_part_button;
    wxButton* mirror_part_button;

    wxStaticText* min_clearance_text;
    wxStaticText* section_view_text;
    wxSpinCtrlDouble* min_clearance_spinner;
    wxCheckBox* section_view_checkbox;
    wxButton* export_button;
    wxButton* stack_button;
    wxGauge* progress_bar;

    wxNotebook* notebook;
    std::vector<wxPanel*> notebook_panels;

    // Part settings tab
    wxStaticText* quantity_text;
    wxStaticText* min_hole_text;
    wxStaticText* minimize_text;
    wxSpinCtrl* quantity_spinner;
    wxSpinCtrl* min_hole_spinner;
    wxCheckBox* minimize_checkbox;
    wxStaticText* rotation_text;
    wxChoice* rotation_dropdown;
    wxButton* preview_voxelization_button;
    wxButton* preview_bounding_box_button;

    // Sinterbox tab
    wxStaticText* clearance_text;
    wxStaticText* spacing_text;
    wxStaticText* thickness_text;
    wxStaticText* width_text;
    wxStaticText* generate_text;
    wxSpinCtrlDouble* clearance_spinner;
    wxSpinCtrlDouble* spacing_spinner;
    wxSpinCtrlDouble* thickness_spinner;
    wxSpinCtrlDouble* width_spinner;
    wxCheckBox* sinterbox_checkbox;

    // Bounding box tab
    wxStaticText* initial_x_text;
    wxStaticText* initial_y_text;
    wxStaticText* initial_z_text;
    wxStaticText* maximum_x_text;
    wxStaticText* maximum_y_text;
    wxStaticText* maximum_z_text;
    wxSpinCtrl* initial_x_spinner;
    wxSpinCtrl* initial_y_spinner;
    wxSpinCtrl* initial_z_spinner;
    wxSpinCtrl* maximum_x_spinner;
    wxSpinCtrl* maximum_y_spinner;
    wxSpinCtrl* maximum_z_spinner;
};

} // namespace pstack::gui

#endif // PSTACK_GUI_CONTROLS_HPP
