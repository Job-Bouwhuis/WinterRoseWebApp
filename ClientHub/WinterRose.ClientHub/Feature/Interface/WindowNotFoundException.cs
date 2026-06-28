using System;
using WinterRose.DependancyInjection;

namespace WinterRose.ClientHub.Feature.Interface;

internal class WindowNotFoundException<T>() : Exception($"Window of type {typeof(T).Name} not registered");