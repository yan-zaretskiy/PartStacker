#include "pstack/gui/main_window.hpp"
#include "pstack/version.hpp"
#include <wx/app.h>
#include <format>
#include <string>

namespace pstack::gui {

class part_stacker : public wxApp {
public:
    bool OnInit() override {
        if (not wxApp::OnInit()) {
            return false;
        }

        const std::string version_string = (version::patch == 0)
            ? std::format("v{}.{}", version::major, version::minor)
            : std::format("v{}.{}.{}", version::major, version::minor, version::patch);

        auto main = new main_window("PartStacker Community Edition " + version_string);
        main->Show();
        main->after_show();

        return true;
    }
};

} // namespace pstack::gui

wxIMPLEMENT_APP(pstack::gui::part_stacker);
