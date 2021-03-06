﻿using System.Collections.Generic;
using System.Linq;

using Rant.Compiler;
using Rant.Stringes;
using Rant.Stringes.Tokens;

namespace Rant.Blueprints
{
    internal sealed class FuncTagBlueprint : Blueprint
    {
        public RantPattern Source { get; private set; }
        public Stringe Name { get; private set; }

        private readonly RantFunc _tagDef;

        private readonly Argument[] _args;

        public FuncTagBlueprint(VM interpreter, RantPattern source, Stringe name, IEnumerable<Token<R>>[] argData = null)
            : base(interpreter)
        {
            Source = source;
            Name = name.Trim();

            RantFuncSigs defs;

            if (!RantFuncs.F.TryGetValue(Name.Value, out defs))
            {
                throw new RantException(Source, Name, "A function of the name '" + Name.Value + "' does not exist.");
            }

            if ((_tagDef = defs.GetSignature((argData == null ? 0 : argData.Length))) == null)
            {
                throw new RantException(Source, Name, "The function '" + Name.Value + "' has no signature that accepts " + (argData == null ? 0 : argData.Length) + " argument(s).");
            }

            if (argData == null)
            {
                _args = new Argument[0];
            }
            else
            {
                var lastType = _tagDef.Parameters.Last();

                // Insert token arguments into the array, set string args to null.
                _args = argData.Select((a, i) => 
                    (i >= _tagDef.ParamCount 
                    ? lastType.HasFlag(ParamFlags.Code) // Covers multi params
                    : _tagDef.Parameters[i].HasFlag(ParamFlags.Code))
                        ? Argument.FromTokens(a) 
                        : null).ToArray();

                // Queue string arguments on the stack.
                for (int i = 0; i < argData.Length; i++)
                {
                    if ((i >= _tagDef.ParamCount && !lastType.HasFlag(ParamFlags.Code)) || !_tagDef.Parameters[i].HasFlag(ParamFlags.Code))
                    {
                        interpreter.PushState(VM.State.CreateSub(source, argData[i], interpreter));
                    }
                }
            }
        }

        public override bool Use()
        {
            // Fill in empty string arguments with state results.
            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i] == null) _args[i] = Argument.FromString(I.PopResultString());
            }

            // Call the tag
            return _tagDef.Invoke(I, Source, Name, _args);
        }
    }
}