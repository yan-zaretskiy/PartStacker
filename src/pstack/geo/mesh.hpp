#ifndef PSTACK_GEO_MESH_HPP
#define PSTACK_GEO_MESH_HPP

#include "pstack/geo/functions.hpp"
#include "pstack/geo/triangle.hpp"
#include <vector>

namespace pstack::geo {

class mesh {
private:
    std::vector<triangle> _triangles{};

public:
    constexpr mesh() = default;
    constexpr mesh(std::vector<triangle> triangles)
        : _triangles(std::move(triangles)) {}
    
    constexpr const auto& triangles() const {
        return _triangles;
    }

    struct bounding_t {
        point3<float> min;
        point3<float> max;
        vector3<int> box_size;
    };

    constexpr bounding_t bounding() const {
        bounding_t out;
        out.min = { std::numeric_limits<float>::max(), std::numeric_limits<float>::max(), std::numeric_limits<float>::max() };
        out.max = { std::numeric_limits<float>::min(), std::numeric_limits<float>::min(), std::numeric_limits<float>::min() };

        for (const auto& triangle : _triangles) {
            out.min = min(out.min, triangle.v1);
            out.min = min(out.min, triangle.v2);
            out.min = min(out.min, triangle.v3);
            out.max = max(out.max, triangle.v1);
            out.max = max(out.max, triangle.v2);
            out.max = max(out.max, triangle.v3);
        }

        const vector3<float> size = out.max - out.min;
        out.box_size = { ceil(size.x + 2), ceil(size.y + 2), ceil(size.z + 2) };
        return out;
    }

    struct volume_and_centroid_t {
        double volume;
        point3<float> centroid;
    };

    constexpr volume_and_centroid_t volume_and_centroid() const {
        double total_volume = 0;
        vector3<float> total_centroid = origin3<float>.as_vector();
        for (const auto& t : _triangles) {
            const auto volume_piece = dot(t.v1.as_vector(), cross(t.v2.as_vector(), t.v3.as_vector()));
            total_volume += volume_piece;
            total_centroid += volume_piece * (t.v1.as_vector() + t.v2.as_vector() + t.v3.as_vector());
        }
        // The `/6` and `/4` should actually go on the two calculation lines above,
        // but they're factored out to the final result for efficiency. 
        return { .volume = total_volume / 6, .centroid = ((total_centroid / 4) / total_volume) + origin3<float> };
    }
};

} // namespace pstack::geo

#endif // PSTACK_GEO_MESH_HPP
