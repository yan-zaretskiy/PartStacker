#ifndef PSTACK_CALC_VOXELIZE_HPP
#define PSTACK_CALC_VOXELIZE_HPP

#include "pstack/calc/mesh.hpp"
#include "pstack/util/mdarray.hpp"

namespace pstack::calc {

int voxelize(const mesh& mesh, util::mdspan<int, 3> voxels, int index, std::size_t carver_size);

} // namespace pstack::calc

#endif // PSTACK_CALC_VOXELIZE_HPP
