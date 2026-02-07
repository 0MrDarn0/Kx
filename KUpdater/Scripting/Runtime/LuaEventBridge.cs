// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Runtime {
    public class LuaEventBridge {
        private readonly Script _lua;
        private readonly IEventManager _eventManager;

        public LuaEventBridge(IEventManager eventManager, Script lua) {
            _eventManager = eventManager;
            _lua = lua;
        }

        public void Register<T>(string luaFunctionName) {
            _eventManager.Register<T>(ev => {
                var fn = _lua.Globals.Get(luaFunctionName);
                if (fn.Type == DataType.Function) {
                    _lua.Call(fn, ev);
                }
            });
        }
    }
}
