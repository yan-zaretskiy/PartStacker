#include "pstack/gui/main_window.hpp"
#include "pstack/version.hpp"
#include <wx/app.h>
#include <wx/string.h>
#include <string>

namespace pstack::gui {

class part_stacker : public wxApp {
public:
    bool OnInit() override {
        if (not wxApp::OnInit()) {
            return false;
        }

        const wxString version_string = (version::patch == 0)
            ? wxString::Format("v%d.%d", version::major, version::minor)
            : wxString::Format("v%d.%d.%d", version::major, version::minor, version::patch);

        auto main = new main_window("PartStacker Community Edition " + version_string);
        main->Show();

        return true;
    }
};

} // namespace pstack::gui

wxIMPLEMENT_APP(pstack::gui::part_stacker);
