#ifndef PSTACK_CALC_VOXELIZE_HPP
#define PSTACK_CALC_VOXELIZE_HPP

#include "pstack/geo/mesh.hpp"
#include <mdspan>

namespace pstack::calc {

int voxelize(const geo::mesh& mesh, std::mdspan<int, std::dextents<std::size_t, 3>> voxels, int index, std::size_t carver_size);

} // namespace pstack::calc

#endif // PSTACK_CALC_VOXELIZE_HPP
