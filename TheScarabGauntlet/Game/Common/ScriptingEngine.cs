using System;
using System.IO;
using System.Reflection;
using LuaInterface;

namespace Scripting
{
    public class ScriptingEngine
    {
        #region Private Members
        private Lua luaVM;
        private static ScriptingEngine instance;
        #endregion

        #region Public Properties

        public static ScriptingEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new ScriptingEngine();

                return instance;
            }
        }
        
        #endregion

        #region Public Routines

        /// <summary>
        /// Register the object and all exposed methods with the scripting engine.
        /// </summary>
        /// <param name="target">The class to register.</param>
        public void RegisterObject(object target)
        {
            if (luaVM == null)
            {
                Console.WriteLine("Lua VM wasn't created");
                return;
            }
             
            Type targetType = target.GetType();

            foreach (MethodInfo info in targetType.GetMethods())
            {
                foreach (Attribute attr in info.GetCustomAttributes(true))// Attribute.GetCustomAttributes(info))
                {
                    if (attr.GetType() == typeof(LuaFuncAttr))
                    {
                        LuaFuncAttr luaAttr = (LuaFuncAttr)attr;

                        luaVM.RegisterFunction(luaAttr.Name, target, info);
                    }
                }
            }
        }

        public void RegisterFunction(string name, object target)
        {
            luaVM.RegisterFunction(name, target, target.GetType().GetMethod(name));
        }

        /// <summary>
        /// Runs a command through the scripting engine and returns
        /// any data associated with that command.
        /// </summary>
        /// <param name="command">The scripting command to run.</param>
        /// <returns>The objects produced by the inputed scripting command.</returns>
        public object[] RunCommand(string command)
        {
            try
            {
                return luaVM.DoString(command);
            }
            catch (LuaException lex)
            {
                Console.WriteLine(lex);
            }

            return null;
        }

        /// <summary>
        /// Runs a predefined script through the scripting engine.
        /// </summary>
        /// <param name="script">The script to run.</param>
        /// <returns>The possible data returned from the script.</returns>
        public object[] RunScript(string script)
        {
            if (File.Exists(script))
            {
                try
                {
                    return luaVM.DoFile(script);
                }
                catch (LuaException lex)
                {
                    Console.WriteLine(lex);
                }
            }
            else
                Console.WriteLine("Script does not exist!!");

            return null;
        }

        #endregion

        #region Private Routines

        private ScriptingEngine()
        {
            luaVM = new Lua();
        }

        #endregion
    }

    public class LuaFuncAttr : Attribute
    {
        #region Private Members
        private string functionName;
        private string functionDoc;
        #endregion

        #region Public Properties

        public string Name
        {
            get { return functionName; }
        }

        public string Notes
        {
            get { return functionDoc; }
        }

        #endregion

        #region Public Routines

        public LuaFuncAttr(string funcName, string funcDoc)
        {
            functionName = funcName;
            functionDoc = funcDoc;
        }

        #endregion
    }
}
