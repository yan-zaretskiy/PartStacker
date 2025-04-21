#include "pstack/files/stl.hpp"
#include "pstack/gui/constants.hpp"
#include "pstack/gui/main_window.hpp"
#include "pstack/gui/parts_list.hpp"
#include "pstack/gui/viewport.hpp"

#include <cstdlib>
#include <wx/colourdata.h>
#include <wx/filedlg.h>
#include <wx/gbsizer.h>
#include <wx/menu.h>
#include <wx/msgdlg.h>
#include <wx/sizer.h>

namespace pstack::gui {

main_window::main_window(const wxString& title)
    : wxFrame(nullptr, wxID_ANY, title, wxDefaultPosition, wxDefaultSize)
{
#ifdef _WIN32
    SetBackgroundColour(constants::form_background_colour);
#endif

    SetMenuBar(make_menu_bar());

    _controls.initialize(this);
    _parts_list.initialize(_controls.notebook_panels[0]);
    bind_all_controls();
    enable_part_settings(false);

    wxGLAttributes attrs;
    attrs.PlatformDefaults().Defaults().EndList();
    if (not wxGLCanvas::IsDisplaySupported(attrs)) {
        wxMessageBox("wxGLCanvas::IsDisplaySupported() returned false", "PartStacker fatal error", wxICON_ERROR);
        std::exit(EXIT_FAILURE);
    }
    _viewport = new viewport(this, attrs);
    _viewport->SetMinSize(FromDIP(constants::min_viewport_size));

    auto sizer = new wxBoxSizer(wxHORIZONTAL);
    sizer->Add(_viewport, 1, wxEXPAND);
    sizer->Add(arrange_all_controls(), 0, wxEXPAND | wxALL, FromDIP(constants::outer_border));
    SetSizerAndFit(sizer);
}

void main_window::on_select_parts(const std::vector<std::size_t>& indices) {
    const auto size = indices.size();
    _controls.delete_button->Enable(size != 0);
    _controls.reload_button->Enable(size != 0);
    _controls.copy_button->Enable(size == 1);
    _controls.mirror_button->Enable(size == 1);
    if (size == 1) {
        set_part(indices[0]);
    } else {
        unset_part();
    }
}

void main_window::set_part(const std::size_t index) {
    enable_part_settings(true);
    _current_part = &_parts_list.at(index);
    _current_part_index.emplace(index);
    _controls.quantity_spinner->SetValue(_current_part->quantity);
    _controls.min_hole_spinner->SetValue(_current_part->min_hole);
    _controls.minimize_checkbox->SetValue(_current_part->rotate_min_box);
    _controls.rotation_dropdown->SetSelection(_current_part->rotation_index);
    _viewport->set_mesh(_current_part->mesh, _current_part->centroid);
}

void main_window::unset_part() {
    enable_part_settings(false);
    _current_part = nullptr;
    _current_part_index = std::nullopt;
    return;
}

void main_window::enable_part_settings(bool enable) {
    _controls.quantity_spinner->Enable(enable);
    _controls.min_hole_spinner->Enable(enable);
    _controls.minimize_checkbox->Enable(enable);
    _controls.rotation_dropdown->Enable(enable);
    _controls.preview_voxelization_button->Enable(enable);
    _controls.preview_bounding_box_button->Enable(enable);
}

void main_window::on_stacking(wxCommandEvent& event) {
    if (_controls.stack_button->GetLabelText() == "Stop") {
        on_stacking_stop();
    } else {
        on_stacking_start();
    }
    event.Skip();
}

void main_window::on_stacking_start() {
    const auto triangles = _parts_list.total_triangles();
    if (triangles == 0) {
        return;
    } else if (triangles > 1'000'000) {
        if (wxMessageBox("The finished model will exceed 1,000,000 triangles. Continue stacking?", "Warning", wxYES_NO | wxYES_DEFAULT | wxICON_INFORMATION) != wxYES) {
            return;
        }
    }

    calc::stack_parameters params {
        .parts = _parts_list.get_all(),

        .set_progress = [this](double progress, double total) {
            CallAfter([=] {
                _controls.progress_bar->SetValue(static_cast<int>(100 * progress / total));
            });
        },
        .display_mesh = [this](const calc::mesh& mesh, int max_x, int max_y, int max_z) {
            CallAfter([=] {
                // Make a copy of `mesh`, otherwise we encounter a data race
                _viewport->set_mesh(mesh, { max_x / 2.0f, max_y / 2.0f, max_z / 2.0f });
            });
        },
        .on_success = [this](calc::mesh mesh, const std::chrono::duration<double> elapsed) {
            CallAfter([=, mesh = std::move(mesh)] {
                on_stacking_success(std::move(mesh), elapsed);
                _controls.export_button->Enable();
            });
        },
        .on_failure = [this] {
            CallAfter([=] {
                _controls.export_button->Disable();
                _last_result.reset();
                wxMessageBox("Could not stack parts within maximum bounding box", "Stacking failed");
            });
        },
        .on_finish = [this] {
            CallAfter([=] {
                _stacker_thread.stop();
                enable_on_stacking(false);
            });
        },

        .resolution = _controls.min_clearance_spinner->GetValue(),
        .x_min = _controls.initial_x_spinner->GetValue(), .x_max = _controls.maximum_x_spinner->GetValue(),
        .y_min = _controls.initial_y_spinner->GetValue(), .y_max = _controls.maximum_y_spinner->GetValue(),
        .z_min = _controls.initial_z_spinner->GetValue(), .z_max = _controls.maximum_z_spinner->GetValue(),
    };
    enable_on_stacking(true);
    _stacker_thread.start(std::move(params));
}

void main_window::on_stacking_stop() {
    if (wxMessageBox("Abort stacking?", "Warning", wxYES_NO | wxNO_DEFAULT | wxICON_WARNING) == wxYES) {
        _stacker_thread.stop();
    }
}

void main_window::on_stacking_success(calc::mesh mesh, const std::chrono::duration<double> elapsed) {
    _last_result.emplace(std::move(mesh));
    auto bounding = _last_result->bounding();
    if (_controls.sinterbox_checkbox->GetValue()) {
        const double offset = _controls.thickness_spinner->GetValue() + _controls.clearance_spinner->GetValue();
        _last_result->set_baseline(geo::origin3<float> + offset);
        const calc::sinterbox_parameters params {
            .min = bounding.min + offset,
            .max = bounding.max + offset,
            .clearance = _controls.clearance_spinner->GetValue(),
            .thickness = _controls.thickness_spinner->GetValue(),
            .width = _controls.width_spinner->GetValue(),
            .spacing = _controls.spacing_spinner->GetValue() + 0.00013759,
        };
        _last_result->add_sinterbox(params);
        bounding = _last_result->bounding();
    }

    const auto size = bounding.max - bounding.min;
    const auto centroid = (size / 2) + geo::origin3<float>;
    _viewport->set_mesh(*_last_result, centroid);

    const double volume = _last_result->volume_and_centroid().volume;
    const double percent_volume = 100 * volume / (size.x * size.y * size.z);

    const auto message = wxString::Format(
        "Stacking complete!\n\nElapsed time: %.1fs\n\nFinal bounding box: %.1fx%.1fx%.1fmm (%.1f%% density).\n\nWould you like to save the result now?",
        elapsed.count(), size.x, size.y, size.z, percent_volume);
    if (wxMessageBox(message, "Stacking complete", wxYES_NO | wxYES_DEFAULT | wxICON_INFORMATION) == wxYES) {
        on_export();
    }
}

void main_window::enable_on_stacking(const bool starting) {
    const bool enable = not starting;
    enable_part_settings(enable and _current_part_index.has_value());
    _parts_list.control()->Enable(enable);
    for (wxMenuItem* item : _disableable_menu_items) {
        item->Enable(enable);
    }

    if (enable) {
        static thread_local std::vector<std::size_t> selected{};
        _parts_list.get_selected(selected);
        _controls.import_button->Enable();
        _controls.delete_button->Enable(selected.size() != 0);
        _controls.reload_button->Enable(selected.size() != 0);
        _controls.copy_button->Enable(selected.size() != 0);
        _controls.mirror_button->Enable(selected.size() != 0);
    } else {
        _controls.import_button->Disable();
        _controls.delete_button->Disable();
        _controls.reload_button->Disable();
        _controls.copy_button->Disable();
        _controls.mirror_button->Disable();
    }

    _controls.clearance_spinner->Enable(enable);
    _controls.spacing_spinner->Enable(enable);
    _controls.thickness_spinner->Enable(enable);
    _controls.width_spinner->Enable(enable);
    _controls.sinterbox_checkbox->Enable(enable);

    _controls.initial_x_spinner->Enable(enable);
    _controls.initial_y_spinner->Enable(enable);
    _controls.initial_z_spinner->Enable(enable);
    _controls.maximum_x_spinner->Enable(enable);
    _controls.maximum_y_spinner->Enable(enable);
    _controls.maximum_z_spinner->Enable(enable);

    _controls.min_clearance_spinner->Enable(enable);
    _controls.section_view_checkbox->Enable(enable);
    // _controls.export_button is handled elsewhere
    _controls.stack_button->SetLabelText(enable ? "Stack" : "Stop");
    _controls.progress_bar->SetValue(0);
}

wxMenuBar* main_window::make_menu_bar() {
    auto menu_bar = new wxMenuBar();
    enum class menu_item {
        _ = 0, // Menu items cannot be 0 on Mac
        new_, open, save, close,
        import, export_,
        about, website,
    };
    menu_bar->Bind(wxEVT_MENU, [this](wxCommandEvent& event) {
        switch (menu_item{ event.GetId() }) {
            case menu_item::new_: {
                return on_new(event);
            }
            case menu_item::open: {
                wxMessageBox("Not yet implemented", "Error", wxICON_WARNING);
                break;
            }
            case menu_item::save: {
                wxMessageBox("Not yet implemented", "Error", wxICON_WARNING);
                break;
            }
            case menu_item::close: {
                Close();
                break;
            }
            case menu_item::import: {
                return on_import(event);
            }
            case menu_item::export_: {
                return on_export(event);
            }
            case menu_item::about: {
                constexpr auto str =
                    "PartStacker Community Edition\n\n"
                    "PartStacker Community Edition is a continuation of PartStacker, (c)opyright Tom van der Zanden 2011-2013.\nVisit https://github.com/TomvdZanden/PartStacker/.\n\n"
                    "PartStacker Community Edition is (c)opyright Braden Ganetsky 2025.\nVisit https://github.com/PartStackerCommunity/PartStacker/.\n\n"
                    "Both the original and the Community Edition are licensed under the GNU General Public License v3.0.";
                wxMessageBox(str, "PartStacker Community Edition");
                break;
            }
            case menu_item::website: {
                wxLaunchDefaultBrowser("https://github.com/PartStackerCommunity/PartStacker/");
                break;
            }
        }
        event.Skip();
    });

    auto file_menu = new wxMenu();
    _disableable_menu_items.push_back(file_menu->Append((int)menu_item::new_, "&New\tCtrl-N", "Clear the current working session"));
    _disableable_menu_items.push_back(file_menu->Append((int)menu_item::open, "&Open\tCtrl-O", "Open PartStacker settings file"));
    _disableable_menu_items.push_back(file_menu->Append((int)menu_item::save, "&Save\tCtrl-S", "Save PartStacker settings"));
    file_menu->Append((int)menu_item::close, "&Close\tShift-Esc", "Close PartStacker");
    menu_bar->Append(file_menu, "&File");

    auto import_menu = new wxMenu();
    _disableable_menu_items.push_back(import_menu->Append((int)menu_item::import, "&Import\tCtrl-I", "Open mesh files"));
    _disableable_menu_items.push_back(import_menu->Append((int)menu_item::export_, "&Export\tCtrl-E", "Save last result as mesh file"));
    menu_bar->Append(import_menu, "&Mesh");

    auto help_menu = new wxMenu();
    help_menu->Append((int)menu_item::about, "&About", "About PartStacker");
    help_menu->Append((int)menu_item::website, "Visit &website", "Open PartStacker GitHub");
    menu_bar->Append(help_menu, "&Help");

    return menu_bar;
}

void main_window::bind_all_controls() {
    Bind(wxEVT_CLOSE_WINDOW, &main_window::on_close, this);

    _parts_list.bind([this](const std::vector<std::size_t>& selected) {
        return on_select_parts(selected);
    });

    _controls.import_button->Bind(wxEVT_BUTTON, &main_window::on_import, this);
    _controls.delete_button->Bind(wxEVT_BUTTON, &main_window::on_delete, this);
    _controls.reload_button->Bind(wxEVT_BUTTON, &main_window::on_reload, this);
    _controls.copy_button->Bind(wxEVT_BUTTON, [this](wxCommandEvent& event) {
        _parts_list.append(*_current_part);
        _parts_list.update_label();
        event.Skip();
    });
    _controls.mirror_button->Bind(wxEVT_BUTTON, [this](wxCommandEvent& event) {
        _current_part->mirrored = not _current_part->mirrored;
        _current_part->mesh.mirror_x();
        _current_part->mesh.set_baseline({ 0, 0, 0 });
        _parts_list.reload_text(_current_part_index.value());
        set_part(_current_part_index.value());
        event.Skip();
    });

    _controls.stack_button->Bind(wxEVT_BUTTON, &main_window::on_stacking, this);
    _controls.export_button->Bind(wxEVT_BUTTON, &main_window::on_export, this);

    _controls.quantity_spinner->Bind(wxEVT_SPINCTRL, [this](wxSpinEvent& event) {
        _current_part->quantity = event.GetPosition();
        event.Skip();
        _parts_list.reload_quantity(_current_part_index.value());
    });
    _controls.min_hole_spinner->Bind(wxEVT_SPINCTRL, [this](wxSpinEvent& event) {
        _current_part->min_hole = event.GetPosition();
        event.Skip();
    });
    _controls.minimize_checkbox->Bind(wxEVT_CHECKBOX, [this](wxCommandEvent& event) {
        _current_part->rotate_min_box = event.IsChecked();
        event.Skip();
    });

    _controls.rotation_dropdown->Bind(wxEVT_CHOICE, [this](wxCommandEvent& event) {
        _current_part->rotation_index = _controls.rotation_dropdown->GetSelection();
        event.Skip();
    });

    _controls.preview_voxelization_button->Bind(wxEVT_BUTTON, [this](wxCommandEvent& event) {
        wxMessageBox("Not yet implemented", "Error", wxICON_WARNING);
    });
    _controls.preview_bounding_box_button->Bind(wxEVT_BUTTON, [this](wxCommandEvent& event) {
        wxMessageBox("Not yet implemented", "Error", wxICON_WARNING);
    });

    _controls.section_view_checkbox->Bind(wxEVT_CHECKBOX, [this](wxCommandEvent& event) {
        wxMessageBox("Not yet implemented", "Error", wxICON_WARNING);
    });
}

void main_window::on_new(wxCommandEvent& event) {
    if (_parts_list.rows() == 0 or
        wxMessageBox("Clear the current working session?",
                     "Warning",
                     wxYES_NO | wxNO_DEFAULT | wxICON_INFORMATION) == wxYES)
    {
        _controls.reset_values();
        _parts_list.delete_all();
        unset_part();
        _viewport->remove_mesh();
        _last_result.reset();
        _controls.export_button->Disable();
    }
    event.Skip();
}

void main_window::on_close(wxCloseEvent& event) {
    if (_parts_list.rows() != 0 and event.CanVeto()) {
        if (wxMessageBox("Close PartStacker?",
                         "Warning",
                         wxYES_NO | wxNO_DEFAULT | wxICON_INFORMATION) != wxYES)
        {
            event.Veto();
            return;
        }
    }
    _stacker_thread.stop();
    event.Skip();
}

void main_window::on_export(wxCommandEvent& event) {
    on_export();
    event.Skip();
}

void main_window::on_export() {
    if (not _last_result.has_value()) {
        wxMessageBox("Nothing to export", "Error", wxICON_WARNING);
        return;
    }

    wxFileDialog dialog(this, "Export mesh", "", "",
                        "STL files (*.stl)|*.stl",
                        wxFD_SAVE | wxFD_OVERWRITE_PROMPT);

    if (dialog.ShowModal() == wxID_CANCEL) {
        return;
    }

    const wxString path = dialog.GetPath();
    files::to_stl(*_last_result, path.ToStdString());
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
        _parts_list.append(path.ToStdString());
    }
    _parts_list.update_label();
    if (paths.size() == 1) {
        auto& part = _parts_list.at(_parts_list.rows() - 1);
        _viewport->set_mesh(part.mesh, part.centroid);
    }

    event.Skip();
}

void main_window::on_delete(wxCommandEvent& event) {
    static thread_local std::vector<std::size_t> selected{};
    _parts_list.get_selected(selected);
    const auto message = wxString::Format("Delete %s %zu item%s?", selected.size() == 1 ? "this" : "these", selected.size(), selected.size() == 1 ? "" : "s");
    wxMessageDialog dialog(this, message, "Warning", wxYES_NO | wxNO_DEFAULT | wxICON_WARNING);
    if (dialog.ShowModal() == wxID_YES) {
        _parts_list.delete_selected();
        _parts_list.update_label();
        on_select_parts({});
    }
    event.Skip();
}

void main_window::on_reload(wxCommandEvent& event) {
    static thread_local std::vector<std::size_t> selected{};
    _parts_list.get_selected(selected);
    for (const std::size_t row : selected) {
        _parts_list.reload_file(row);
    }
    _parts_list.update_label();
    on_select_parts({});
    event.Skip();
}

wxSizer* main_window::arrange_all_controls() {
    auto sizer = new wxBoxSizer(wxVERTICAL);
    sizer->Add(arrange_tabs(), 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(arrange_bottom_section1(), 0, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(arrange_bottom_section2(), 0, wxEXPAND);
    return sizer;
}

wxSizer* main_window::arrange_part_buttons() {
    auto sizer = new wxBoxSizer(wxHORIZONTAL);
    _controls.import_button->SetMinSize(FromDIP(constants::min_button_size));
    _controls.delete_button->SetMinSize(FromDIP(constants::min_button_size));
    _controls.reload_button->SetMinSize(FromDIP(constants::min_button_size));
    _controls.copy_button->SetMinSize(FromDIP(constants::min_button_size));
    _controls.mirror_button->SetMinSize(FromDIP(constants::min_button_size));
    sizer->Add(_controls.import_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.delete_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.reload_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.copy_button, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.mirror_button, 1, wxEXPAND);
    return sizer;
}

wxSizer* main_window::arrange_bottom_section1() {
    auto sizer = new wxBoxSizer(wxHORIZONTAL);
    sizer->Add(_controls.section_view_text, 0, wxALIGN_CENTER_VERTICAL);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.section_view_checkbox, 0, wxALIGN_CENTER_VERTICAL);
    sizer->AddStretchSpacer();
    sizer->Add(_controls.export_button, 0, wxALIGN_CENTER_VERTICAL);
    return sizer;
}

wxSizer* main_window::arrange_bottom_section2() {
    auto sizer = new wxBoxSizer(wxHORIZONTAL);
    _controls.progress_bar->SetMinSize(FromDIP(constants::min_button_size));
    _controls.stack_button->SetMinSize(FromDIP(constants::min_button_size));
    sizer->Add(_controls.progress_bar, 1, wxEXPAND);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(_controls.stack_button);
    return sizer;
}

wxNotebook* main_window::arrange_tabs() {
    arrange_tab_part_settings(_controls.notebook_panels[0]);
    arrange_tab_stack_settings(_controls.notebook_panels[1]);
    arrange_tab_results(_controls.notebook_panels[2]);
    return _controls.notebook;
}

void main_window::arrange_tab_part_settings(wxPanel* panel) {
    auto sizer = new wxBoxSizer(wxVERTICAL);
    panel->SetSizer(sizer);

    sizer->Add(_parts_list.control(), 1, wxEXPAND);
    sizer->AddSpacer(panel->FromDIP(constants::inner_border));
    sizer->Add(arrange_part_buttons(), 0, wxEXPAND);
    sizer->AddSpacer(panel->FromDIP(constants::inner_border));
    sizer->Add(_parts_list.label());
    sizer->AddSpacer(panel->FromDIP(constants::inner_border));

    auto lower_sizer = new wxBoxSizer(wxHORIZONTAL);
    sizer->Add(lower_sizer, 0, wxEXPAND);

    {
        auto label_sizer = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        label_sizer->Add(_controls.quantity_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(_controls.min_hole_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(_controls.minimize_text, 0, wxALIGN_CENTER_VERTICAL);

        auto spinner_sizer = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        spinner_sizer->Add(_controls.quantity_spinner, 0, wxALIGN_CENTER_VERTICAL);
        spinner_sizer->Add(_controls.min_hole_spinner, 0, wxALIGN_CENTER_VERTICAL);
        spinner_sizer->Add(_controls.minimize_checkbox, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        auto rotation_sizer = new wxBoxSizer(wxHORIZONTAL);
        rotation_sizer->Add(_controls.rotation_text, 0, wxALIGN_CENTER_VERTICAL);
        rotation_sizer->AddSpacer(panel->FromDIP(2 * constants::inner_border));
        rotation_sizer->Add(_controls.rotation_dropdown, 0, wxALIGN_CENTER_VERTICAL);
        button_sizer->Add(rotation_sizer);
        button_sizer->Add(_controls.preview_voxelization_button);
        button_sizer->Add(_controls.preview_bounding_box_button);

        lower_sizer->Add(label_sizer, 0, wxEXPAND);
        lower_sizer->AddSpacer(panel->FromDIP(3 * constants::inner_border));
        lower_sizer->Add(spinner_sizer, 0, wxEXPAND);
        lower_sizer->AddSpacer(panel->FromDIP(6 * constants::inner_border));
        lower_sizer->Add(button_sizer, 0, wxEXPAND);
        lower_sizer->AddStretchSpacer();
    }
}

void main_window::arrange_tab_stack_settings(wxPanel* panel) {
    auto sizer = new wxBoxSizer(wxVERTICAL);
    panel->SetSizer(sizer);

    auto bounding_box_sizer_ = new wxStaticBoxSizer(wxHORIZONTAL, panel, "Bounding box");
    {
        auto bounding_box_sizer = new wxBoxSizer(wxHORIZONTAL);
        bounding_box_sizer_->Add(bounding_box_sizer, 0, wxALL, panel->FromDIP(constants::inner_border));

        auto label_sizer1 = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        label_sizer1->Add(_controls.initial_x_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer1->Add(_controls.initial_y_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer1->Add(_controls.initial_z_text, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer1 = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        button_sizer1->Add(_controls.initial_x_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer1->Add(_controls.initial_y_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer1->Add(_controls.initial_z_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        auto label_sizer2 = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        label_sizer2->Add(_controls.maximum_x_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer2->Add(_controls.maximum_y_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer2->Add(_controls.maximum_z_text, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer2 = new wxGridSizer(3, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        button_sizer2->Add(_controls.maximum_x_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer2->Add(_controls.maximum_y_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer2->Add(_controls.maximum_z_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        bounding_box_sizer->Add(label_sizer1, 0, wxEXPAND);
        bounding_box_sizer->AddSpacer(panel->FromDIP(4 * constants::inner_border));
        bounding_box_sizer->Add(button_sizer1, 0, wxEXPAND);
        bounding_box_sizer->AddSpacer(panel->FromDIP(8 * constants::inner_border));
        bounding_box_sizer->Add(label_sizer2, 0, wxEXPAND);
        bounding_box_sizer->AddSpacer(panel->FromDIP(4 * constants::inner_border));
        bounding_box_sizer->Add(button_sizer2, 0, wxEXPAND);
        bounding_box_sizer->AddStretchSpacer();
    }

    auto min_clearance_sizer = new wxBoxSizer(wxHORIZONTAL);
    min_clearance_sizer->Add(_controls.min_clearance_text, 0, wxALIGN_CENTER_VERTICAL);
    min_clearance_sizer->AddSpacer(4 * FromDIP(constants::inner_border));
    min_clearance_sizer->Add(_controls.min_clearance_spinner, 0, wxALIGN_CENTER_VERTICAL);

    sizer->Add(bounding_box_sizer_, 0, wxEXPAND | wxLEFT | wxRIGHT);
    sizer->AddSpacer(FromDIP(constants::inner_border));
    sizer->Add(min_clearance_sizer);
}

void main_window::arrange_tab_results(wxPanel* panel) {
    auto sizer = new wxBoxSizer(wxVERTICAL);
    panel->SetSizer(sizer);

    auto sinterbox_sizer_ = new wxStaticBoxSizer(wxHORIZONTAL, panel, "Sinterbox");
    {
        auto sinterbox_sizer = new wxBoxSizer(wxHORIZONTAL);
        sinterbox_sizer_->Add(sinterbox_sizer, 0, wxALL, panel->FromDIP(constants::inner_border));

        auto label_sizer = new wxGridSizer(4, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        label_sizer->Add(_controls.clearance_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(_controls.spacing_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(_controls.thickness_text, 0, wxALIGN_CENTER_VERTICAL);
        label_sizer->Add(_controls.width_text, 0, wxALIGN_CENTER_VERTICAL);

        auto button_sizer = new wxGridSizer(4, 1, panel->FromDIP(constants::inner_border), panel->FromDIP(constants::inner_border));
        button_sizer->Add(_controls.clearance_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(_controls.spacing_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(_controls.thickness_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);
        button_sizer->Add(_controls.width_spinner, 0, wxALIGN_CENTER_VERTICAL | wxEXPAND);

        auto checkbox_sizer = new wxBoxSizer(wxHORIZONTAL);
        checkbox_sizer->Add(_controls.generate_text, 0, wxALIGN_CENTER_VERTICAL);
        checkbox_sizer->AddSpacer(panel->FromDIP(2 * constants::inner_border));
        checkbox_sizer->Add(_controls.sinterbox_checkbox, 0, wxALIGN_CENTER_VERTICAL);

        sinterbox_sizer->Add(label_sizer, 0, wxEXPAND);
        sinterbox_sizer->AddSpacer(panel->FromDIP(8 * constants::inner_border + 2));
        sinterbox_sizer->Add(button_sizer, 0, wxEXPAND);
        sinterbox_sizer->AddSpacer(panel->FromDIP(2 * constants::inner_border));
        sinterbox_sizer->Add(checkbox_sizer, 0, wxALIGN_LEFT | wxALIGN_TOP | wxUP, panel->FromDIP(4));
        sinterbox_sizer->AddStretchSpacer();
    }

    sizer->Add(sinterbox_sizer_, 0, wxEXPAND | wxLEFT | wxRIGHT);
}

} // namespace pstack::gui
