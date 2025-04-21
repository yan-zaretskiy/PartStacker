#ifndef PSTACK_CALC_PART_HPP
#define PSTACK_CALC_PART_HPP

#include <string>
#include "pstack/calc/mesh.hpp"

namespace pstack::calc {

struct part {
    std::string mesh_file;
    std::string name;
    mesh mesh;

    int quantity;

    double volume;
    geo::point3<float> centroid;
    int triangle_count;
    bool mirrored;
    int min_hole;
    int rotation_index;
    bool rotate_min_box;
};

} // namespace pstack::calc

#endif // PSTACK_CALC_PART_HPP
