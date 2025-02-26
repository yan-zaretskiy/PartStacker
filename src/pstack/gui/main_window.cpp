#include "pstack/files/stl.hpp"
#include "pstack/gui/main_window.hpp"
#include "pstack/gui/parts_list.hpp"
#include "pstack/gui/part_properties.hpp"
#include "pstack/gui/viewport.hpp"

#include <wx/button.h>
#include <wx/checkbox.h>
#include <wx/colourdata.h>
#include <wx/filedlg.h>
#include <wx/gauge.h>
#include <wx/gbsizer.h>
#include <wx/listctrl.h>
#include <wx/menu.h>
#include <wx/msgdlg.h>
#include <wx/notebook.h>
#include <wx/panel.h>
#include <wx/radiobut.h>
#include <wx/sizer.h>
#include <wx/spinctrl.h>
#include <wx/stattext.h>

namespace pstack::gui {

constexpr int outside_border = 20;
constexpr int tab_padding = 7;
constexpr int inner_border = 5;
constexpr int button_height = 25;
const wxSize button_size = wxSize(-1, button_height);
const wxColour background_colour = wxColour(0xF0, 0xF0, 0xF0);

main_window::main_window(const wxString& title)
    : wxFrame(nullptr, wxID_ANY, title, wxDefaultPosition, wxDefaultSize)
{
    SetBackgroundColour(background_colour);
    SetMenuBar(make_menu_bar());

    auto sizer = new wxBoxSizer(wxHORIZONTAL);

    wxGLAttributes attrs;
    attrs.PlatformDefaults().Defaults().EndList();

    if (wxGLCanvas::IsDisplaySupported(attrs)) {
        _viewport = new viewport(this, attrs);
        _viewport->SetMinSize(FromDIP(wxSize(640, -1)));
        sizer->Add(_viewport, 1, wxEXPAND);
    }

    _parts_list.update_label();

    auto right_sizer = new wxBoxSizer(wxVERTICAL);
    right_sizer->Add(_parts_list.control(), 1, wxEXPAND);
    right_sizer->AddSpacer(FromDIP(inner_border));
    right_sizer->Add(make_part_buttons(), 0, wxEXPAND);
    right_sizer->AddSpacer(FromDIP(inner_border));
    right_sizer->Add(_parts_list.label());
    right_sizer->AddSpacer(FromDIP(inner_border));
    right_sizer->Add(make_tabs(this), 0, wxEXPAND);
    right_sizer->AddSpacer(FromDIP(inner_border));
    right_sizer->Add(make_bottom_section1(this), 0, wxEXPAND);
    right_sizer->AddSpacer(FromDIP(inner_border));
    right_sizer->Add(make_bottom_section2(this), 0, wxEXPAND);

    sizer->Add(right_sizer, 0, wxEXPAND | wxALL, FromDIP(outside_border));
    SetSizerAndFit(sizer);
}

void main_window::after_show() {
    _viewport->on_move_by({0, 0});
    _viewport->render();
}

void main_window::select_parts(const std::vector<std::size_t>& indices) {
    const auto size = indices.size();
    _delete_button->Enable(size != 0);
    _change_button->Enable(size == 1);
    _reload_button->Enable(size != 0);
    if (size == 1) {
        set_part(indices[0]);
    } else {
        unset_part();
    }
}

void main_window::set_part(const std::size_t index) {
    enable_part_settings(true);
    _current_part = &_parts_list.at(index);
    _quantity_spinner->SetValue(_current_part->quantity);
    _min_hole_spinner->SetValue(_current_part->min_hole);
    _minimize_checkbox->SetValue(_current_part->rotate_min_box);
    switch (_current_part->rotation_index) {
        case 0: {
            _radio_none->SetValue(true);
            _radio_cubic->SetValue(false);
            _radio_arbitrary->SetValue(false);
            break;
        }
        case 1: {
            _radio_none->SetValue(false);
            _radio_cubic->SetValue(true);
            _radio_arbitrary->SetValue(false);
            break;
        }
        case 2: {
            _radio_none->SetValue(false);
            _radio_cubic->SetValue(false);
            _radio_arbitrary->SetValue(true);
            break;
        }
        default: std::unreachable();
    }
    _viewport->set_mesh(_current_part->mesh, _current_part->centroid);
    _viewport->render();
}

void main_window::unset_part() {
    enable_part_settings(false);
    _current_part = nullptr;
    return;
}

void main_window::enable_part_settings(bool enable) {
    _quantity_spinner->Enable(enable);
    _min_hole_spinner->Enable(enable);
    _minimize_checkbox->Enable(enable);
    _radio_none->Enable(enable);
    _radio_arbitrary->Enable(enable);
    _radio_cubic->Enable(enable);
}

wxMenuBar* main_window::make_menu_bar() {
    auto menu_bar = new wxMenuBar();

    auto file_menu = new wxMenu();
    file_menu->Append(wxID_ANY, "New");
    file_menu->Append(wxID_ANY, "Open");
    file_menu->Append(wxID_ANY, "Save");
    file_menu->Append(wxID_ANY, "Close");
    menu_bar->Append(file_menu, "File");

    auto import_menu = new wxMenu();
    import_menu->Append(wxID_ANY, "Import parts");
    import_menu->Append(wxID_ANY, "Export result as STL");
    menu_bar->Append(import_menu, "Import/Export");

    auto help_menu = new wxMenu();
    help_menu->Append(wxID_ANY, "About");
    help_menu->Append(wxID_ANY, "Visit website");
    menu_bar->Append(help_menu, "Help");

    return menu_bar;
}

void main_window::on_import(wxCommandEvent& event) {
    wxFileDialog dialog(this, "Import mesh", "", "",
                        "STL files (*.stl)|*.stl",
                        wxFD_OPEN | wxFD_FILE_MUST_EXIST | wxFD_MULTIPLE);

    if (dialog.ShowModal() == wxID_CANCEL) {
        return;
    }

    wxArrayString paths;
    dialog.GetPaths(paths);

    for (const auto& path : paths) {
        _parts_list.append_row(path.ToStdString());
    }
    _parts_list.update_label();
    if (paths.size() == 1) {
        set_part(_parts_list.rows() - 1);
    } else {
        unset_part();
    }
    
    event.Skip();
}

void main_window::on_delete(wxCommandEvent& event) {
    static thread_local std::vector<std::size_t> selected{};
    _parts_list.get_selected(selected);
    auto message = std::format("Are you sure you want to delete {} {} item{}?", selected.size() == 1 ? "this" : "these", selected.size(), selected.size() == 1 ? "" : "s");
    auto dialog = new wxMessageDialog(this, std::move(message), "Delete items", wxYES_NO | wxNO_DEFAULT);
    if (dialog->ShowModal() == wxID_YES) {
        _parts_list.delete_selected();
        _parts_list.update_label();
        select_parts({});
    }
    event.Skip();
}

void main_window::on_change(wxCommandEvent& event) {
    // todo
    event.Skip();
}

void main_window::on_reload(wxCommandEvent& event) {
    // todo
    event.Skip();
}

wxSizer* main_window::make_part_buttons() {
    auto sizer = new wxBoxSizer(wxHORIZONTAL);
    const auto make_button = [this](const char* text, bool enable, void(main_window::*fn)(wxCommandEvent&)) {
        auto button = new wxButton(this, wxID_ANY, text);
        button->SetMinSize(FromDIP(button_size));
        button->Bind(wxEVT_BUTTON, fn, this);
        button->Enable(enable);
        return button;
    };
    _import_button = make_button("Import", true, &main_window::on_import);
    _delete_button = make_button("Delete", false, &main_window::on_delete);
    _change_button = make_button("Change", false, &main_window::on_change);
    _reload_button = make_button("Reload", false, &main_window::on_reload);
    sizer->Add(_import_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(inner_border));
    sizer->Add(_delete_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(inner_border));
    sizer->Add(_change_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(inner_border));
    sizer->Add(_reload_button, 1, wxEXPAND);
    return sizer;
}

wxSizer* main_window::make_bottom_section1(main_window* frame) {
    auto sizer = new wxGridBagSizer(frame->FromDIP(inner_border), frame->FromDIP(inner_border));

    auto text1 = new wxStaticText(frame, wxID_ANY, "Minimum clearance:");
    auto text2 = new wxStaticText(frame, wxID_ANY, "Section view:");
    sizer->Add(text1, wxGBPosition(0, 0), wxDefaultSpan, wxALIGN_CENTER_VERTICAL);
    sizer->Add(text2, wxGBPosition(1, 0), wxDefaultSpan, wxALIGN_CENTER_VERTICAL);

    auto spinner = new wxSpinCtrlDouble(frame);
    spinner->SetDigits(2);
    spinner->SetIncrement(0.05);
    spinner->SetRange(0.5, 2);
    spinner->SetValue(1);
    sizer->Add(spinner, wxGBPosition(0, 1), wxDefaultSpan, wxALIGN_CENTER_VERTICAL);

    auto checkbox = new wxCheckBox(frame, wxID_ANY, "");
    sizer->Add(checkbox, wxGBPosition(1, 1), wxDefaultSpan, wxALIGN_CENTER_VERTICAL);

    new wxGBSizerItem(sizer, wxGBPosition(1, 2), wxDefaultSpan, wxEXPAND);

    auto export_button = new wxButton(frame, wxID_ANY, "Export result as STL");
    export_button->SetMinSize(frame->FromDIP(button_size));
    sizer->Add(export_button, wxGBPosition(1, 3));
    
    sizer->AddGrowableCol(2, 1);
    return sizer;
}

wxSizer* main_window::make_bottom_section2(main_window* frame) {
    auto sizer = new wxBoxSizer(wxHORIZONTAL);

    auto progress = new wxGauge(frame, wxID_ANY, 100);
    progress->SetMinSize(frame->FromDIP(button_size));

    auto start_button = new wxButton(frame, wxID_ANY, "Start");
    start_button->SetMinSize(frame->FromDIP(button_size));

    sizer->Add(progress, 1, wxEXPAND);
    sizer->AddSpacer(frame->FromDIP(inner_border));
    sizer->Add(start_button);

    return sizer;
}

wxWindow* main_window::make_tabs(main_window* frame) {
    auto notebook = new wxNotebook(frame, wxID_ANY);

    {
        auto panel_base = new wxPanel(notebook);
        auto panel_base_sizer = new wxBoxSizer(wxVERTICAL);
        panel_base->SetSizer(panel_base_sizer);
        notebook->InsertPage(0, panel_base, "Part Settings");
        auto panel = new wxPanel(panel_base);
        panel_base_sizer->Add(panel, 1, wxEXPAND | wxALL, notebook->FromDIP(tab_padding));

        auto panel_sizer = new wxBoxSizer(wxVERTICAL);
        panel->SetSizer(panel_sizer);
        auto top_sizer = new wxBoxSizer(wxHORIZONTAL);
        auto bottom_sizer = new wxBoxSizer(wxHORIZONTAL);
        panel_sizer->Add(top_sizer, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(inner_border));
        panel_sizer->Add(bottom_sizer);

        {
            auto label_sizer = new wxGridSizer(3, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
            auto text1 = new wxStaticText(panel, wxID_ANY, "Quantity:");
            auto text2 = new wxStaticText(panel, wxID_ANY, "Minimum hole:");
            auto text3 = new wxStaticText(panel, wxID_ANY, "Minimize box:");
            label_sizer->Add(text1, 0, wxALIGN_CENTER_VERTICAL);
            label_sizer->Add(text2, 0, wxALIGN_CENTER_VERTICAL);
            label_sizer->Add(text3, 0, wxALIGN_CENTER_VERTICAL);

            auto button_sizer = new wxGridSizer(3, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
            auto make_spinner = [&panel](int maximum) {
                auto spinner = new wxSpinCtrl(panel);
                spinner->SetRange(0, maximum);
                spinner->SetValue(1);
                return spinner;
            };
            frame->_quantity_spinner = make_spinner(200);
            frame->_min_hole_spinner = make_spinner(100);
            frame->_minimize_checkbox = new wxCheckBox(panel, wxID_ANY, "");
            button_sizer->Add(frame->_quantity_spinner, 0, wxALIGN_CENTER_VERTICAL);
            button_sizer->Add(frame->_min_hole_spinner, 0, wxALIGN_CENTER_VERTICAL);
            button_sizer->Add(frame->_minimize_checkbox, 0, wxALIGN_CENTER_VERTICAL);
            frame->_quantity_spinner->Bind(wxEVT_SPINCTRL, [frame](wxSpinEvent& event) {
                frame->_current_part->quantity = event.GetPosition();
                frame->_parts_list.refresh_quantity_text();
            });
            frame->_min_hole_spinner->Bind(wxEVT_SPINCTRL, [frame](wxSpinEvent& event) { frame->_current_part->min_hole = event.GetPosition(); });
            frame->_minimize_checkbox->Bind(wxEVT_CHECKBOX, [frame](wxCommandEvent& event) { frame->_current_part->rotate_min_box = event.IsChecked(); });

            auto radio_box = new wxStaticBoxSizer(wxVERTICAL, panel, "Part rotations");
            auto radio_sizer = new wxGridSizer(2, 2, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
            radio_box->Add(radio_sizer, 1, wxEXPAND | wxALL, notebook->FromDIP(tab_padding));
            frame->_radio_none = new wxRadioButton(panel, wxID_ANY, "None");
            frame->_radio_arbitrary = new wxRadioButton(panel, wxID_ANY, "Arbitrary");
            frame->_radio_cubic = new wxRadioButton(panel, wxID_ANY, "Cubic");
            radio_sizer->Add(frame->_radio_none, 0, wxEXPAND);
            radio_sizer->Add(frame->_radio_arbitrary, 0, wxEXPAND);
            radio_sizer->Add(frame->_radio_cubic, 0, wxEXPAND);
            frame->_radio_none->Bind(wxEVT_RADIOBUTTON, [frame](wxCommandEvent& event) { frame->_current_part->rotation_index = 0; });
            frame->_radio_arbitrary->Bind(wxEVT_RADIOBUTTON, [frame](wxCommandEvent& event) { frame->_current_part->rotation_index = 2; });
            frame->_radio_cubic->Bind(wxEVT_RADIOBUTTON, [frame](wxCommandEvent& event) { frame->_current_part->rotation_index = 1; });

            frame->enable_part_settings(false);
            top_sizer->Add(label_sizer, 0, wxEXPAND);
            top_sizer->AddSpacer(notebook->FromDIP(3 * inner_border));
            top_sizer->Add(button_sizer, 0, wxEXPAND);
            top_sizer->AddStretchSpacer();
            top_sizer->Add(radio_box, 0, wxEXPAND);
        }

        {
            auto preview_button = new wxButton(panel, wxID_ANY, "Preview");
            preview_button->SetMinSize(notebook->FromDIP(button_size));
            auto mirrored_button = new wxButton(panel, wxID_ANY, "Mirrored");
            mirrored_button->SetMinSize(notebook->FromDIP(button_size));
            bottom_sizer->Add(preview_button);
            bottom_sizer->AddSpacer(notebook->FromDIP(inner_border));
            bottom_sizer->Add(mirrored_button);
        }
    }

    {
        auto panel_base = new wxPanel(notebook);
        auto panel_base_sizer = new wxBoxSizer(wxVERTICAL);
        panel_base->SetSizer(panel_base_sizer);
        notebook->InsertPage(1, panel_base, "Sinterbox");
        auto panel = new wxPanel(panel_base);
        panel_base_sizer->Add(panel, 1, wxEXPAND | wxALL, notebook->FromDIP(tab_padding));

        auto panel_sizer = new wxBoxSizer(wxHORIZONTAL);
        panel->SetSizer(panel_sizer);

        auto label_sizer = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto text1 = new wxStaticText(panel, wxID_ANY, "Clearance:");
        auto text2 = new wxStaticText(panel, wxID_ANY, "Spacing:");
        auto text3 = new wxStaticText(panel, wxID_ANY, "Thickness:");
        auto text4 = new wxStaticText(panel, wxID_ANY, "Width:");
        label_sizer->Add(text1, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(text2, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(text3, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(text4, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto make_spinner = [&panel](double minimum, double maximum, double value, double increment) {
            auto spinner = new wxSpinCtrlDouble(panel);
            spinner->SetIncrement(increment);
            spinner->SetRange(minimum, maximum);
            spinner->SetValue(value);
            return spinner;
        };
        auto updown1 = make_spinner(0.1, 4, 0.8, 0.1);
        auto updown2 = make_spinner(1, 20, 6, 0.5);
        auto updown3 = make_spinner(0.1, 4, 0.8, 0.1);
        auto updown4 = make_spinner(0.1, 4, 1.1, 0.1);
        button_sizer->Add(updown1, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(updown2, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(updown3, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(updown4, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        auto checkbox_sizer = new wxBoxSizer(wxHORIZONTAL);
        auto generate_text = new wxStaticText(panel, wxID_ANY, "Generate sinterbox:");
        auto checkbox = new wxCheckBox(panel, wxID_ANY, "");
        checkbox->SetValue(true);
        checkbox_sizer->Add(generate_text, 0, wxALIGN_CENTER_VERTICAL);
        checkbox_sizer->AddSpacer(notebook->FromDIP(2 * inner_border));
        checkbox_sizer->Add(checkbox, 0, wxALIGN_CENTER_VERTICAL);

        panel_sizer->Add(label_sizer, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(8 * inner_border + 2));
        panel_sizer->Add(button_sizer, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(2 * inner_border));
        panel_sizer->Add(checkbox_sizer, 0, wxALIGN_LEFT | wxALIGN_TOP | wxUP, notebook->FromDIP(4));
    }

    {
        auto panel_base = new wxPanel(notebook);
        auto panel_base_sizer = new wxBoxSizer(wxVERTICAL);
        panel_base->SetSizer(panel_base_sizer);
        notebook->InsertPage(2, panel_base, "Bounding Box");
        auto panel = new wxPanel(panel_base);
        panel_base_sizer->Add(panel, 1, wxEXPAND | wxALL, notebook->FromDIP(tab_padding));

        auto panel_sizer = new wxBoxSizer(wxHORIZONTAL);
        panel->SetSizer(panel_sizer);

        auto make_spinner = [&panel](double value) {
            auto spinner = new wxSpinCtrl(panel);
            spinner->SetRange(10, 250);
            spinner->SetValue(value);
            return spinner;
        };

        auto label_sizer1 = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto text1 = new wxStaticText(panel, wxID_ANY, "Initial X:");
        auto text2 = new wxStaticText(panel, wxID_ANY, "Initial Y:");
        auto text3 = new wxStaticText(panel, wxID_ANY, "Initial Z:");
        label_sizer1->Add(text1, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer1->Add(text2, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer1->Add(text3, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer1 = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto updown1 = make_spinner(150);
        auto updown2 = make_spinner(150);
        auto updown3 = make_spinner(30);
        button_sizer1->Add(updown1, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer1->Add(updown2, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer1->Add(updown3, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        auto label_sizer2 = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto text4 = new wxStaticText(panel, wxID_ANY, "Maximum X:");
        auto text5 = new wxStaticText(panel, wxID_ANY, "Maximum Y:");
        auto text6 = new wxStaticText(panel, wxID_ANY, "Maximum Z:");
        label_sizer2->Add(text4, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer2->Add(text5, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer2->Add(text6, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer2 = new wxGridSizer(4, 1, notebook->FromDIP(inner_border), notebook->FromDIP(inner_border));
        auto updown4 = make_spinner(156);
        auto updown5 = make_spinner(156);
        auto updown6 = make_spinner(90);
        button_sizer2->Add(updown4, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer2->Add(updown5, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer2->Add(updown6, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        panel_sizer->Add(label_sizer1, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(4 * inner_border));
        panel_sizer->Add(button_sizer1, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(8 * inner_border));
        panel_sizer->Add(label_sizer2, 0, wxEXPAND);
        panel_sizer->AddSpacer(notebook->FromDIP(4 * inner_border));
        panel_sizer->Add(button_sizer2, 0, wxEXPAND);
        panel_sizer->AddStretchSpacer();
    }

    return notebook;
}

} // namespace pstack::gui
