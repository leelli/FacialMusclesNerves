#ifndef MESHMERGE_FORWARD_PLUS_KEYWORD_INCLUDED
#define MESHMERGE_FORWARD_PLUS_KEYWORD_INCLUDED

// URP 14 (Unity 2022.3) uses the _FORWARD_PLUS keyword for Forward+ rendering.
// URP 17 (Unity 6) deprecated _FORWARD_PLUS in favour of _CLUSTER_LIGHT_LOOP.
// Emit the correct keyword for the active engine so the custom shader receives
// the main-light data in Forward+ mode (otherwise objects render dark/unlit).
#if UNITY_VERSION >= 600000
    #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#else
    #pragma multi_compile _ _FORWARD_PLUS
#endif

#endif // MESHMERGE_FORWARD_PLUS_KEYWORD_INCLUDED
