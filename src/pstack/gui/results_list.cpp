#include "pstack/gui/results_list.hpp"

namespace pstack::gui {

void results_list::initialize(wxWindow* parent) {
    list_view::initialize(parent, {
        { "Timestamp", 105 },
        { "Volume", 60 },
        { "Triangles", 60 },
        { "Sinterbox", 90 },
    });
}

} // namespace pstack::gui
