#ifndef PSTACK_FILES_STL_HPP
#define PSTACK_FILES_STL_HPP

#include "pstack/geo/mesh.hpp"
#include <string>

namespace pstack::files {

geo::mesh from_stl(const std::string& from_file);
void to_stl(const geo::mesh& mesh, const std::string& to_file);

} // namespace pstack::files

#endif // PSTACK_FILES_STL_HPP
