using Unity.Entities;
using Unity.Mathematics;

/**
 * Trees have:
 * - A position
 * - An age (which increments every tick)
 * - A predetermined death age (upon which they are culled)
 * - A predetermined mature age (when reached, they start propogating)
*/

/**
 * A forest:
 * - Increments the age for each tree
 * - Culls all trees whose age > death
 * - Culls all trees in competition of each other
 *      - Competition is radius-based. If two trees are within a distance of each other
 *        then they are in competition. The smaller of the two trees dies. This is to simulate
 *        canopy cover: bigger trees occlude light to smaller plants; which die. 
 * - Produces new trees:
 *      - If a tree is mature (age > mature), then there is a chance it will spread its seed
 *      - If this chance is satisfied, then a new tree is spread from the original's location
 * - Has:
 *      - A list of trees 
 *      - A wind direction
 *      - An initial population of trees, randomly distributed
 *      - Parameters for min/max mature & death ages
 *      - Parameters for min/max spread chances
 *      - Parameters for min/max tree spread distances
 *      - Min/max cull regions (so a forest can be generated in a region)
 */

/* The forest simulation:
 * - Initially populates the region with a list of trees; they are randomly distributed points
 * - Continues to step until terminated (either by iter count or time-based). For each step:
 *      - Randomise wind direction
 *      - Cull trees:
 *          - Tag trees whose age > death as dead
 *          - Delete dead trees
 *      - Age trees:
 *          - Increment age of all alive trees
 *          - Set tree as mature if age > matureAge
 *      - Assess competition:
 *         - For every alive tree, look for neighbours in a radius R of the tree.
 *         - If any neighbours age > this tree's age, this tree dies
 *      - Propogate trees:
 *          - For every mature tree, generate a random number
 *          - If that number is more than the forest's spread chance:
 *              - Find offset point from tree's pos P with sin/cos, theta being wind direction
 *              - Make a new tree here and add it to the list of trees
 *              
 */

/// <summary>
/// Represents a single tree within the simulation
/// </summary>
public struct TreeComponent : IComponentData
{
    public float2 m_position;
    public uint m_age;
    public uint m_deathAge;
    public uint m_matureAge;
}

/// <summary>
/// A tag component to identify trees needing cull
/// </summary>
public struct DeadTreeTagComponent : IComponentData { }

/// <summary>
/// A tag component to identify trees who are mature and can propogate
/// </summary>
public struct MatureTreeTagComponent : IComponentData { }