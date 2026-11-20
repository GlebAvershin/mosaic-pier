using System.Runtime.InteropServices;

public static class Rules
{
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_start(int chapter);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_tile(int cell);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_frost(int cell);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_clear(int cell);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_fall(int cell);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_moves();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_left();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_score();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_chain();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_sweeps();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_shuffle();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_status();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_swap(int a, int b);
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_step();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_hint();
    [DllImport("mosaic", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mp_count();
}
