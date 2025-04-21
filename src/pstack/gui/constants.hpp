#ifndef PSTACK_GUI_CONSTANTS_HPP
#define PSTACK_GUI_CONSTANTS_HPP

#include <wx/colour.h>
#include <wx/gdicmn.h>

namespace pstack::gui {

struct constants {

static constexpr int tab_padding = 7;
static constexpr int outer_border = 20;
static constexpr int inner_border = 5;

inline static const wxSize min_button_size{72, 25};
inline static const wxSize min_list_size{380, 200};
inline static const wxSize min_viewport_size{640, -1};

#ifdef _WIN32
inline static const wxColour form_background_colour{0xF0, 0xF0, 0xF0};
#endif

};

} // namespace pstack::gui

#endif // PSTACK_GUI_CONSTANTS_HPP
