#ifndef PSTACK_GUI_PART_PROPERTIES_HPP
#define PSTACK_GUI_PART_PROPERTIES_HPP

#include <string>
#include "pstack/geo/mesh.hpp"

namespace pstack::gui {

struct part_properties {
    std::string mesh_file;
    std::string name;
    geo::mesh mesh;

    int quantity;

    double volume;
    geo::point3<float> centroid;
    int triangle_count;
    bool mirrored;
    int min_hole;
    int rotation_index;
    bool rotate_min_box;
};

} // namespace pstack::gui

#endif PSTACK_GUI_PART_PROPERTIES_HPP
