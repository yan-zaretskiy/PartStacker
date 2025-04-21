#include "pstack/gui/controls.hpp"
#include "pstack/gui/main_window.hpp"

namespace pstack::gui {

namespace {

std::pair<wxNotebook*, std::vector<wxPanel*>> make_tab_panels(wxWindow* parent, const std::vector<const char*> labels) {
    wxNotebook* const notebook = new wxNotebook(parent, wxID_ANY);
    std::vector<wxPanel*> panels;
    for (std::size_t i = 0; i != labels.size(); ++i) {
        wxPanel* const panel_base = new wxPanel(notebook);
        wxBoxSizer* const panel_base_sizer = new wxBoxSizer(wxVERTICAL);
        panel_base->SetSizer(panel_base_sizer);
        notebook->InsertPage(i, panel_base, labels[i]);
        wxPanel* const panel = new wxPanel(panel_base);
        panel_base_sizer->Add(panel, 1, wxEXPAND | wxALL, notebook->FromDIP(tab_padding));
        panels.push_back(panel);
    }
    return { notebook, std::move(panels) };
}

} // namespace

void controls::initialize(main_window* parent) {
    import_button = new wxButton(parent, wxID_ANY, "Import");
    delete_button = new wxButton(parent, wxID_ANY, "Delete");
    delete_button->Disable();
    change_button = new wxButton(parent, wxID_ANY, "Change");
    change_button->Disable();
    reload_button = new wxButton(parent, wxID_ANY, "Reload");
    reload_button->Disable();

    min_clearance_text = new wxStaticText(parent, wxID_ANY, "Minimum clearance:");
    section_view_text = new wxStaticText(parent, wxID_ANY, "Section view:");
    min_clearance_spinner = new wxSpinCtrlDouble(parent);
    min_clearance_spinner->SetDigits(2);
    min_clearance_spinner->SetIncrement(0.05);
    min_clearance_spinner->SetRange(0.5, 2);
    section_view_checkbox = new wxCheckBox(parent, wxID_ANY, "");
    export_button = new wxButton(parent, wxID_ANY, "Export result as STL");
    export_button->Disable();
    progress_bar = new wxGauge(parent, wxID_ANY, 100);
    stack_button = new wxButton(parent, wxID_ANY, "Start");
    
    std::tie(notebook, notebook_panels) = make_tab_panels(parent, {
        "Part Settings",
        "Sinterbox",
        "Bounding Box"
    });
    
    {
        const auto panel = notebook_panels[0];
        quantity_text = new wxStaticText(panel, wxID_ANY, "Quantity:");
        min_hole_text = new wxStaticText(panel, wxID_ANY, "Minimum hole:");
        minimize_text = new wxStaticText(panel, wxID_ANY, "Minimize box:");
        quantity_spinner = new wxSpinCtrl(panel);
        quantity_spinner->SetRange(0, 200);
        min_hole_spinner = new wxSpinCtrl(panel);
        min_hole_spinner->SetRange(0, 100);
        minimize_checkbox = new wxCheckBox(panel, wxID_ANY, "");
        radio_none = new wxRadioButton(panel, wxID_ANY, "None");
        radio_arbitrary = new wxRadioButton(panel, wxID_ANY, "Arbitrary");
        radio_cubic = new wxRadioButton(panel, wxID_ANY, "Cubic");
        preview_button = new wxButton(panel, wxID_ANY, "Preview");
        copy_button = new wxButton(panel, wxID_ANY, "Copy");
        mirror_button = new wxButton(panel, wxID_ANY, "Mirror");
    }

    {
        const auto panel = notebook_panels[1];
        clearance_text = new wxStaticText(panel, wxID_ANY, "Clearance:");
        spacing_text = new wxStaticText(panel, wxID_ANY, "Spacing:");
        thickness_text = new wxStaticText(panel, wxID_ANY, "Thickness:");
        width_text = new wxStaticText(panel, wxID_ANY, "Width:");
        generate_text = new wxStaticText(panel, wxID_ANY, "Generate sinterbox:");
        const auto make_spinner = [&panel](double minimum, double maximum, double increment) {
            auto spinner = new wxSpinCtrlDouble(panel);
            spinner->SetIncrement(increment);
            spinner->SetRange(minimum, maximum);
            return spinner;
        };
        clearance_spinner = make_spinner(0.1, 4, 0.1);
        spacing_spinner = make_spinner(1, 20, 0.5);
        thickness_spinner = make_spinner(0.1, 4, 0.1);
        width_spinner = make_spinner(0.1, 4, 0.1);
        sinterbox_checkbox = new wxCheckBox(panel, wxID_ANY, "");
    }

    {
        const auto panel = notebook_panels[2];
        initial_x_text = new wxStaticText(panel, wxID_ANY, "Initial X:");
        initial_y_text = new wxStaticText(panel, wxID_ANY, "Initial Y:");
        initial_z_text = new wxStaticText(panel, wxID_ANY, "Initial Z:");
        maximum_x_text = new wxStaticText(panel, wxID_ANY, "Maximum X:");
        maximum_y_text = new wxStaticText(panel, wxID_ANY, "Maximum Y:");
        maximum_z_text = new wxStaticText(panel, wxID_ANY, "Maximum Z:");
        const auto make_spinner = [panel] {
            auto spinner = new wxSpinCtrl(panel);
            spinner->SetRange(10, 250);
            return spinner;
        };
        initial_x_spinner = make_spinner();
        initial_y_spinner = make_spinner();
        initial_z_spinner = make_spinner();
        maximum_x_spinner = make_spinner();
        maximum_y_spinner = make_spinner();
        maximum_z_spinner = make_spinner();
    }
}

void controls::reset_values() {
    min_clearance_spinner->SetValue(1);

    quantity_spinner->SetValue(1);
    min_hole_spinner->SetValue(1);
    minimize_checkbox->SetValue(false);
    radio_none->SetValue(false);
    radio_cubic->SetValue(false);
    radio_arbitrary->SetValue(false);

    clearance_spinner->SetValue(0.8);
    spacing_spinner->SetValue(6.0);
    thickness_spinner->SetValue(0.8);
    width_spinner->SetValue(1.1);
    sinterbox_checkbox->SetValue(true);
    
    initial_x_spinner->SetValue(150);
    initial_y_spinner->SetValue(150);
    initial_z_spinner->SetValue(30);
    maximum_x_spinner->SetValue(156);
    maximum_y_spinner->SetValue(156);
    maximum_z_spinner->SetValue(90);
}

} // namespace pstack::gui
