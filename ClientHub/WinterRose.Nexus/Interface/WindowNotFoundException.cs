using System;

namespace WinterRose.Nexus.Interface;

internal class WindowNotFoundException<T>() : Exception($"Window of type {typeof(T).Name} not registered");