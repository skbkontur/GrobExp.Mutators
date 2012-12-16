//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Emit;
//
//namespace GrobExp
//{
//    public class GrEmit
//    {
//        public GrEmit(DynamicMethod method, bool debugMode)
//        {
//            this.debugMode = debugMode;
//            il = method.GetILGenerator();
//            methodReturnType = method.ReturnType;
//            methodParameterTypes = Formatter.GetParameterTypes(method);
//        }
//
//        public GrEmit(MethodBuilder method, bool debugMode)
//        {
//            this.debugMode = debugMode;
//            il = method.GetILGenerator();
//            methodReturnType = method.ReturnType;
//            Type[] parameterTypes = Formatter.GetParameterTypes(method);
//            methodParameterTypes = method.IsStatic ? parameterTypes : new[] {method.ReflectedType}.Concat(parameterTypes).ToArray();
//        }
//
//        public GrEmit(ConstructorBuilder constructor, bool debugMode)
//        {
//            this.debugMode = debugMode;
//            il = constructor.GetILGenerator();
//            methodReturnType = typeof(void);
//            methodParameterTypes = new[] {constructor.ReflectedType}.Concat(Formatter.GetParameterTypes(constructor)).ToArray();
//        }
//
//        public string GetILCode()
//        {
//            return ilCode.ToString();
//        }
//
//        public Local DeclareLocal(Type localType, string name, bool pinned = false)
//        {
//            return new Local(il.DeclareLocal(localType, pinned), (string.IsNullOrEmpty(name) ? "local" : name) + "_" + locals++);
//        }
//
//        public Local DeclareLocal(Type localType, bool pinned = false)
//        {
//            return new Local(il.DeclareLocal(localType, pinned), "local_" + locals++);
//        }
//
//        public Label DefineLabel(string name)
//        {
//            return new Label(il.DefineLabel(), name + "_" + labels++);
//        }
//
//        public void MarkLabel(Label label)
//        {
//            if(debugMode)
//            {
//                if(stack == null)
//                {
//                    Type[] labelStack;
//                    if(!stacks.TryGetValue(label, out labelStack))
//                        throw new InvalidOperationException("Cannot compute stack for label '" + label + "'");
//                    stack = new Stack<Type>(labelStack);
//                }
//                else
//                {
//                    Type[] labelStack;
//                    if(stacks.TryGetValue(label, out labelStack))
//                        CheckStacksEqual(label, labelStack);
//                    else
//                        stacks.Add(label, stack.Reverse().ToArray());
//                }
//            }
//            ilCode.MarkLabel(label);
//            il.MarkLabel(label);
//        }
//
//        public void Ret()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                if(methodReturnType == typeof(void))
//                {
//                    if(stack.Count != 0)
//                        throw new InvalidOperationException("At the end stack must be empty");
//                }
//                else if(stack.Count == 0)
//                    throw new InvalidOperationException("Stack is empty");
//                else if(stack.Count > 1)
//                    throw new InvalidOperationException("At the end stack must contain exactly one element");
//                else
//                {
//                    var peek = stack.Pop();
//                    CheckCanBeAssigned(methodReturnType, peek);
//                }
//            }
//            Emit(OpCodes.Ret);
//            stack = null;
//        }
//
//        public void Pop()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//            }
//            Emit(OpCodes.Pop);
//        }
//
//        public void Ldloca(Local local)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(local.Type.MakeByRefType());
//            }
//            Emit(OpCodes.Ldloca, local);
//        }
//
//        public void Ldloc(Local local)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(local.Type);
//            }
//            Emit(OpCodes.Ldloc, local);
//        }
//
//        public void Stloc(Local local)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                var peek = stack.Pop();
//                CheckCanBeAssigned(local.Type, peek);
//            }
//            Emit(OpCodes.Stloc, local);
//        }
//
//        public void Ldnull()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(typeof(object));
//            }
//            Emit(OpCodes.Ldnull);
//        }
//
//        public void Initobj(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(!type.IsValueType)
//                throw new ArgumentException("A value type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//            }
//            Emit(OpCodes.Initobj, type);
//        }
//
//        public void Ldarg(int index)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(methodParameterTypes[index]);
//            }
//            switch(index)
//            {
//            case 0:
//                Emit(OpCodes.Ldarg_0);
//                break;
//            case 1:
//                Emit(OpCodes.Ldarg_1);
//                break;
//            case 2:
//                Emit(OpCodes.Ldarg_2);
//                break;
//            case 3:
//                Emit(OpCodes.Ldarg_3);
//                break;
//            default:
//                if(index < 256)
//                    Emit(OpCodes.Ldarg_S, (byte)index);
//                else
//                    Emit(OpCodes.Ldarg, index);
//                break;
//            }
//        }
//
//        public void Ldarga(int index)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(methodParameterTypes[index].MakeByRefType());
//            }
//            if(index < 256)
//                Emit(OpCodes.Ldarga_S, (byte)index);
//            else
//                Emit(OpCodes.Ldarga, index);
//        }
//
//        public void Dup()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                var peek = stack.Peek();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Dup);
//        }
//
//        public void Ldc_IntPtr(IntPtr value)
//        {
//            if(IntPtr.Size == 4)
//                Ldc_I4(value.ToInt32());
//            else
//                Ldc_I8(value.ToInt64());
//        }
//
//        public void Ldc_I4(int value)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(typeof(int));
//            }
//            switch(value)
//            {
//            case 0:
//                Emit(OpCodes.Ldc_I4_0);
//                break;
//            case 1:
//                Emit(OpCodes.Ldc_I4_1);
//                break;
//            case 2:
//                Emit(OpCodes.Ldc_I4_2);
//                break;
//            case 3:
//                Emit(OpCodes.Ldc_I4_3);
//                break;
//            case 4:
//                Emit(OpCodes.Ldc_I4_4);
//                break;
//            case 5:
//                Emit(OpCodes.Ldc_I4_5);
//                break;
//            case 6:
//                Emit(OpCodes.Ldc_I4_6);
//                break;
//            case 7:
//                Emit(OpCodes.Ldc_I4_7);
//                break;
//            case 8:
//                Emit(OpCodes.Ldc_I4_8);
//                break;
//            case -1:
//                Emit(OpCodes.Ldc_I4_M1);
//                break;
//            default:
//                if(value < 128 && value >= -128)
//                    Emit(OpCodes.Ldc_I4_S, (sbyte)value);
//                else
//                    Emit(OpCodes.Ldc_I4, value);
//                break;
//            }
//        }
//
//        public void Ldc_I8(long value)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(typeof(long));
//            }
//            Emit(OpCodes.Ldc_I8, value);
//        }
//
//        public void Ldc_R4(float value)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(typeof(float));
//            }
//            Emit(OpCodes.Ldc_R4, value);
//        }
//
//        public void Ldc_R8(double value)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                stack.Push(typeof(double));
//            }
//            Emit(OpCodes.Ldc_R8, value);
//        }
//
//        public void Ldlen()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                var peek = stack.Pop();
//                if(!peek.IsArray)
//                    throw new InvalidOperationException("An array expected but was '" + peek + "'");
//                stack.Push(typeof(int));
//            }
//            Emit(OpCodes.Ldlen);
//        }
//
//        public void Br(Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(OpCodes.Br, label);
//            stack = null;
//        }
//
//        public void Brfalse(Label label)
//        {
//            if (label == null)
//                throw new ArgumentNullException("label");
//            if (debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if (stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(OpCodes.Brfalse, label);
//        }
//
//        public void Brtrue(Label label)
//        {
//            if (label == null)
//                throw new ArgumentNullException("label");
//            if (debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if (stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(OpCodes.Brtrue, label);
//        }
//
//        public void Ble(Type type, Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(Unsigned(type) ? OpCodes.Ble_Un : OpCodes.Ble, label);
//        }
//
//        public void Bge(Type type, Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(Unsigned(type) ? OpCodes.Bge_Un : OpCodes.Bge, label);
//        }
//
//        public void Blt(Type type, Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(Unsigned(type) ? OpCodes.Blt_Un : OpCodes.Blt, label);
//        }
//
//        public void Bgt(Type type, Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(Unsigned(type) ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
//        }
//
//        public void Bne(Label label)
//        {
//            if(label == null)
//                throw new ArgumentNullException("label");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//
//                Type[] labelStack;
//                if(stacks.TryGetValue(label, out labelStack))
//                    CheckStacksEqual(label, labelStack);
//                else
//                    stacks.Add(label, stack.Reverse().ToArray());
//            }
//            Emit(OpCodes.Bne_Un, label);
//        }
//
//        public void Call(MethodInfo method, Type type = null, Type[] optionalParameterTypes = null)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                var parameterTypes = Formatter.GetParameterTypes(method);
//                for(int i = parameterTypes.Length - 1; i >= 0; --i)
//                {
//                    CheckNotEmpty();
//                    CheckCanBeAssigned(parameterTypes[i], stack.Pop());
//                }
//                if(!method.IsStatic)
//                {
//                    CheckNotEmpty();
//                    CheckNotStruct(stack.Pop());
//                }
//                if(method.ReturnType != typeof(void))
//                    stack.Push(method.ReturnType);
//            }
//            OpCode opCode = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
//            if(opCode == OpCodes.Callvirt)
//            {
//                if(type == null)
//                    throw new ArgumentNullException("type", "Type must be specified for a virtual method call");
//                if(type.IsValueType)
//                    Emit(OpCodes.Constrained, type);
//            }
//            ilCode.Append(opCode, new MethodILInstructionParameter(method), GetStack());
//            il.EmitCall(opCode, method, optionalParameterTypes);
//        }
//
//        public void Calli(CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes = null)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//                for(int i = parameterTypes.Length - 1; i >= 0; --i)
//                {
//                    CheckNotEmpty();
//                    CheckCanBeAssigned(parameterTypes[i], stack.Pop());
//                }
//                if(returnType != typeof(void))
//                    stack.Push(returnType);
//            }
//            ilCode.Append(OpCodes.Calli, GetStack());
//            il.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes, optionalParameterTypes);
//        }
//
//        public void Ldftn(MethodInfo method)
//        {
//            if(method == null)
//                throw new ArgumentNullException("method");
//            if(debugMode)
//                stack.Push(typeof(IntPtr));
//            Emit(OpCodes.Ldftn, method);
//        }
//
//        public void Ldfld(FieldInfo field)
//        {
//            if(field == null)
//                throw new ArgumentNullException("field");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                stack.Push(field.FieldType);
//            }
//            Emit(OpCodes.Ldfld, field);
//        }
//
//        public void Ldflda(FieldInfo field)
//        {
//            if(field == null)
//                throw new ArgumentNullException("field");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                stack.Push(field.FieldType.MakeByRefType());
//            }
//            Emit(OpCodes.Ldflda, field);
//        }
//
//        public void Ldelema(Type elementType)
//        {
//            if(elementType == null)
//                throw new ArgumentNullException("elementType");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(typeof(int), stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                if(!peek.IsArray)
//                    throw new InvalidOperationException("An array expected but was '" + peek + "'");
//                stack.Push(elementType.MakeByRefType());
//            }
//            Emit(OpCodes.Ldelema, elementType);
//        }
//
//        public void Ldelem(Type elementType)
//        {
//            if(elementType == null)
//                throw new ArgumentNullException("elementType");
//            if(IsStruct(elementType))
//            {
//                // struct
//                Ldelema(elementType);
//                Ldobj(elementType);
//                return;
//            }
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(typeof(int), stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                if(!peek.IsArray)
//                    throw new InvalidOperationException("An array expected but was '" + peek + "'");
//                stack.Push(elementType);
//            }
//            if(!elementType.IsValueType) // class
//                Emit(OpCodes.Ldelem_Ref);
//            else
//            {
//                // Primitive
//                switch(Type.GetTypeCode(elementType))
//                {
//                case TypeCode.Boolean:
//                case TypeCode.SByte:
//                    Emit(OpCodes.Ldelem_I1);
//                    break;
//                case TypeCode.Byte:
//                    Emit(OpCodes.Ldelem_U1);
//                    break;
//                case TypeCode.Int16:
//                    Emit(OpCodes.Ldelem_I2);
//                    break;
//                case TypeCode.Int32:
//                    Emit(OpCodes.Ldelem_I4);
//                    break;
//                case TypeCode.Int64:
//                case TypeCode.UInt64:
//                    Emit(OpCodes.Ldelem_I8);
//                    break;
//                case TypeCode.Char:
//                case TypeCode.UInt16:
//                    Emit(OpCodes.Ldelem_U2);
//                    break;
//                case TypeCode.UInt32:
//                    Emit(OpCodes.Ldelem_U4);
//                    break;
//                case TypeCode.Single:
//                    Emit(OpCodes.Ldelem_R4);
//                    break;
//                case TypeCode.Double:
//                    Emit(OpCodes.Ldelem_R8);
//                    break;
//                default:
//                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
//                }
//            }
//        }
//
//        public void Stelem(Type elementType)
//        {
//            if(elementType == null)
//                throw new ArgumentNullException("elementType");
//            if(IsStruct(elementType))
//                throw new InvalidOperationException("To store an item to an array of structs use Ldelema & Stobj instructions");
//
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(elementType, stack.Pop());
//                CheckNotEmpty();
//                CheckCanBeAssigned(typeof(int), stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                if(!peek.IsArray)
//                    throw new InvalidOperationException("An array expected but was '" + peek + "'");
//            }
//            if(!elementType.IsValueType) // class
//                Emit(OpCodes.Stelem_Ref);
//            else
//            {
//                // Primitive
//                switch(Type.GetTypeCode(elementType))
//                {
//                case TypeCode.Boolean:
//                case TypeCode.SByte:
//                case TypeCode.Byte:
//                    Emit(OpCodes.Stelem_I1);
//                    break;
//                case TypeCode.Char:
//                case TypeCode.UInt16:
//                case TypeCode.Int16:
//                    Emit(OpCodes.Stelem_I2);
//                    break;
//                case TypeCode.Int32:
//                case TypeCode.UInt32:
//                    Emit(OpCodes.Stelem_I4);
//                    break;
//                case TypeCode.Int64:
//                case TypeCode.UInt64:
//                    Emit(OpCodes.Stelem_I8);
//                    break;
//                case TypeCode.Single:
//                    Emit(OpCodes.Stelem_R4);
//                    break;
//                case TypeCode.Double:
//                    Emit(OpCodes.Stelem_R8);
//                    break;
//                default:
//                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
//                }
//            }
//        }
//
//        public void Stind(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(IsStruct(type))
//            {
//                Stobj(type);
//                return;
//            }
//
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(type, stack.Pop());
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//            }
//            if(!type.IsValueType) // class
//                Emit(OpCodes.Stind_Ref);
//            else
//            {
//                // Primitive
//                switch(Type.GetTypeCode(type))
//                {
//                case TypeCode.Boolean:
//                case TypeCode.SByte:
//                case TypeCode.Byte:
//                    Emit(OpCodes.Stind_I1);
//                    break;
//                case TypeCode.Int16:
//                case TypeCode.Char:
//                case TypeCode.UInt16:
//                    Emit(OpCodes.Stind_I2);
//                    break;
//                case TypeCode.Int32:
//                case TypeCode.UInt32:
//                    Emit(OpCodes.Stind_I4);
//                    break;
//                case TypeCode.Int64:
//                case TypeCode.UInt64:
//                    Emit(OpCodes.Stind_I8);
//                    break;
//                case TypeCode.Single:
//                    Emit(OpCodes.Stind_R4);
//                    break;
//                case TypeCode.Double:
//                    Emit(OpCodes.Stind_R8);
//                    break;
//                default:
//                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
//                }
//            }
//        }
//
//        public void Ldind(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(IsStruct(type))
//            {
//                Ldobj(type);
//                return;
//            }
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//            }
//            if(!type.IsValueType) // class
//                Emit(OpCodes.Ldind_Ref);
//            else
//            {
//                switch(Type.GetTypeCode(type))
//                {
//                case TypeCode.SByte:
//                    Emit(OpCodes.Ldind_I1);
//                    break;
//                case TypeCode.Byte:
//                case TypeCode.Boolean:
//                    Emit(OpCodes.Ldind_U1);
//                    break;
//                case TypeCode.Int16:
//                    Emit(OpCodes.Ldind_I2);
//                    break;
//                case TypeCode.Char:
//                case TypeCode.UInt16:
//                    Emit(OpCodes.Ldind_U2);
//                    break;
//                case TypeCode.Int32:
//                    Emit(OpCodes.Ldind_I4);
//                    break;
//                case TypeCode.UInt32:
//                    Emit(OpCodes.Ldind_U4);
//                    break;
//                case TypeCode.Int64:
//                case TypeCode.UInt64:
//                    Emit(OpCodes.Ldind_I8);
//                    break;
//                case TypeCode.Single:
//                    Emit(OpCodes.Ldind_R4);
//                    break;
//                case TypeCode.Double:
//                    Emit(OpCodes.Ldind_R8);
//                    break;
//                default:
//                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
//                }
//            }
//        }
//
//        public void Castclass(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(type.IsValueType)
//                throw new ArgumentException("A reference type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(type, stack.Pop());
//                stack.Push(type);
//            }
//            Emit(OpCodes.Castclass, type);
//        }
//
//        public void Unbox_Any(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(!type.IsValueType)
//                throw new ArgumentException("A value type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(typeof(object), stack.Pop());
//                stack.Push(type);
//            }
//            Emit(OpCodes.Unbox_Any, type);
//        }
//
//        public void Box(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(!type.IsValueType)
//                throw new ArgumentException("A value type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(type, stack.Pop());
//                stack.Push(typeof(object));
//            }
//            Emit(OpCodes.Box, type);
//        }
//
//        public void WriteLine(Local local)
//        {
//            il.EmitWriteLine(local);
//        }
//
//        public void WriteLine(string str)
//        {
//            il.EmitWriteLine(str);
//        }
//
//        public void Stobj(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(!type.IsValueType)
//                throw new ArgumentException("A value type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(type, stack.Pop());
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//            }
//            Emit(OpCodes.Stobj, type);
//        }
//
//        public void Ldobj(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(!type.IsValueType)
//                throw new ArgumentException("A value type expected", "type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckIsAddress(stack.Pop());
//                stack.Push(type);
//            }
//            Emit(OpCodes.Ldobj, type);
//        }
//
//        public void Newobj(ConstructorInfo constructor)
//        {
//            if(constructor == null)
//                throw new ArgumentNullException("constructor");
//            if(debugMode)
//            {
//                CheckNotNull();
//                ParameterInfo[] parameterInfos = constructor.GetParameters();
//                for(int i = parameterInfos.Length - 1; i >= 0; --i)
//                {
//                    CheckNotEmpty();
//                    CheckCanBeAssigned(parameterInfos[i].ParameterType, stack.Pop());
//                }
//                stack.Push(constructor.ReflectedType);
//            }
//            Emit(OpCodes.Newobj, constructor);
//        }
//
//        public void Newarr(Type type)
//        {
//            if(type == null)
//                throw new ArgumentNullException("type");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(typeof(int), stack.Pop());
//                stack.Push(type.MakeArrayType());
//            }
//            Emit(OpCodes.Newarr, type);
//        }
//
//        public void Ceq()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                stack.Push(typeof(int));
//            }
//            Emit(OpCodes.Ceq);
//        }
//
//        public void Cgt(Type type)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                stack.Push(typeof(int));
//            }
//            Emit(Unsigned(type) ? OpCodes.Cgt_Un : OpCodes.Cgt);
//        }
//
//        public void Clt(Type type)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                stack.Push(typeof(int));
//            }
//            Emit(Unsigned(type) ? OpCodes.Clt_Un : OpCodes.Clt);
//        }
//
//        public void And()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.And);
//        }
//
//        public void Or()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Or);
//        }
//
//        public void Xor()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Xor);
//        }
//
//        public void Add()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Add);
//        }
//
//        public void Sub()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Sub);
//        }
//
//        public void Mul()
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(OpCodes.Mul);
//        }
//
//        public void Div(Type type)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(Unsigned(type) ? OpCodes.Div_Un : OpCodes.Div);
//        }
//
//        public void Rem(Type type)
//        {
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//                CheckNotEmpty();
//                Type peek = stack.Pop();
//                CheckNotStruct(peek);
//                stack.Push(peek);
//            }
//            Emit(Unsigned(type) ? OpCodes.Rem_Un : OpCodes.Rem);
//        }
//
//        public void Stfld(FieldInfo field)
//        {
//            if(field == null)
//                throw new ArgumentNullException("field");
//            if(debugMode)
//            {
//                CheckNotNull();
//                CheckNotEmpty();
//                CheckCanBeAssigned(field.FieldType, stack.Pop());
//                CheckNotEmpty();
//                CheckNotStruct(stack.Pop());
//            }
//            Emit(OpCodes.Stfld, field);
//        }
//
//        public void Ldstr(string value)
//        {
//            if(debugMode)
//                stack.Push(typeof(string));
//            Emit(OpCodes.Ldstr, value);
//        }
//
//        public class Label
//        {
//            public Label(System.Reflection.Emit.Label label, string name)
//            {
//                this.label = label;
//                this.name = name;
//            }
//
//            public static implicit operator System.Reflection.Emit.Label(Label label)
//            {
//                return label.label;
//            }
//
//            public string Name { get { return name; } }
//
//            private readonly System.Reflection.Emit.Label label;
//            private readonly string name;
//        }
//
//        public class Local
//        {
//            public Local(LocalBuilder localBuilder, string name)
//            {
//                this.localBuilder = localBuilder;
//                this.name = name;
//            }
//
//            public static implicit operator LocalBuilder(Local local)
//            {
//                return local.localBuilder;
//            }
//
//            public string Name { get { return name; } }
//            public Type Type { get { return localBuilder.LocalType; } }
//
//            private readonly LocalBuilder localBuilder;
//            private readonly string name;
//        }
//
//        private bool StacksConsistent(Type[] otherStack)
//        {
//            if(otherStack.Length != stack.Count)
//                return false;
//            var currentStack = stack.Reverse().ToArray();
//            for(int i = 0; i < otherStack.Length; ++i)
//            {
//                Type type1 = currentStack[i];
//                Type type2 = otherStack[i];
//                if(!type1.IsValueType && !type2.IsValueType)
//                    continue;
//                if(IsStruct(type1) || IsStruct(type2))
//                {
//                    if(type1 != type2)
//                        return false;
//                }
//                else if(GetSize(type1) != GetSize(type2))
//                    return false;
//            }
//            return true;
//        }
//
//        private void CheckStacksEqual(Label label, Type[] otherStack)
//        {
//            if(!StacksConsistent(otherStack))
//                throw new InvalidOperationException("Incosistent stack for label '" + label.Name + "'");
//        }
//
//        private void CheckNotNull()
//        {
//            if(stack == null)
//                throw new InvalidOperationException("Inaccessible instruction");
//        }
//
//        private static bool IsStruct(Type type)
//        {
//            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
//        }
//
//        private static void CheckIsAddress(Type peek)
//        {
//            if(!IsAddressType(peek))
//                throw new InvalidOperationException("An address type expected but was '" + peek + "'");
//        }
//
//        private static void CheckCanBeAssigned(Type to, Type from)
//        {
//            if(!CanBeAssigned(to, from))
//                throw new InvalidOperationException("Unable to set value of type '" + from + "' to value of type '" + to + "'");
//        }
//
//        private static bool CanBeAssigned(Type to, Type from)
//        {
//            if(!to.IsValueType && !from.IsValueType)
//                return true;
//            if(!to.IsValueType && from.IsValueType)
//                return false;
//            if(IsStruct(to) || IsStruct(from))
//                return to == from;
//            return GetSize(to) == GetSize(from);
//        }
//
//        private static int GetSize(Type type)
//        {
//            if(type == typeof(IntPtr))
//                return IntPtr.Size;
//            switch(Type.GetTypeCode(type))
//            {
//            case TypeCode.Boolean:
//            case TypeCode.Byte:
//            case TypeCode.SByte:
//            case TypeCode.Char:
//            case TypeCode.Int16:
//            case TypeCode.Int32:
//            case TypeCode.UInt16:
//            case TypeCode.UInt32:
//            case TypeCode.Single:
//                return 4;
//            case TypeCode.Int64:
//            case TypeCode.UInt64:
//            case TypeCode.Double:
//                return 8;
//            case TypeCode.Object:
//                return IntPtr.Size;
//            default:
//                throw new NotSupportedException("Type '" + type + "' is not supported");
//            }
//        }
//
//        private static bool IsAddressType(Type type)
//        {
//            return type.IsByRef || type.IsPointer || type == typeof(IntPtr) || type == (IntPtr.Size == 4 ? typeof(int) : typeof(long));
//        }
//
//        private static void CheckNotStruct(Type type)
//        {
//            if(IsStruct(type))
//                throw new InvalidOperationException("Struct of type '" + type + "' is not valid at this point");
//        }
//
//        private void CheckNotEmpty()
//        {
//            if(stack.Count == 0)
//                throw new InvalidOperationException("Stack is empty");
//        }
//
//        private static bool Unsigned(Type type)
//        {
//            switch(Type.GetTypeCode(type))
//            {
//            case TypeCode.Boolean:
//            case TypeCode.Byte:
//            case TypeCode.Char:
//            case TypeCode.UInt16:
//            case TypeCode.UInt32:
//            case TypeCode.UInt64:
//                return true;
//            case TypeCode.SByte:
//            case TypeCode.Int16:
//            case TypeCode.Int32:
//            case TypeCode.Int64:
//            case TypeCode.Single:
//            case TypeCode.Double:
//                return false;
//            default:
//                throw new NotSupportedException("Type '" + type.Name + "' is not supported");
//            }
//        }
//
//        private StackILInstructionComment GetStack()
//        {
//            return new StackILInstructionComment(stack == null ? null : stack.Reverse().ToArray());
//        }
//
//        private void Emit(OpCode opCode)
//        {
//            ilCode.Append(opCode, GetStack());
//            il.Emit(opCode);
//        }
//
//        private void Emit(OpCode opCode, Local local)
//        {
//            if(local == null)
//                throw new ArgumentNullException("local");
//            ilCode.Append(opCode, new LocalILInstructionParameter(local), GetStack());
//            il.Emit(opCode, local);
//        }
//
//        private void Emit(OpCode opCode, Type type)
//        {
//            ilCode.Append(opCode, new TypeILInstructionParameter(type), GetStack());
//            il.Emit(opCode, type);
//        }
//
//        private void Emit(OpCode opCode, byte value)
//        {
//            ilCode.Append(opCode, new PrimitiveILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, int value)
//        {
//            ilCode.Append(opCode, new PrimitiveILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, long value)
//        {
//            ilCode.Append(opCode, new PrimitiveILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, double value)
//        {
//            ilCode.Append(opCode, new PrimitiveILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, float value)
//        {
//            ilCode.Append(opCode, new PrimitiveILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, string value)
//        {
//            ilCode.Append(opCode, new StringILInstructionParameter(value), GetStack());
//            il.Emit(opCode, value);
//        }
//
//        private void Emit(OpCode opCode, Label label)
//        {
//            ilCode.Append(opCode, new LabelILInstructionParameter(label), GetStack());
//            il.Emit(opCode, label);
//        }
//
//        private void Emit(OpCode opCode, FieldInfo field)
//        {
//            ilCode.Append(opCode, new FieldILInstructionParameter(field), GetStack());
//            il.Emit(opCode, field);
//        }
//
//        private void Emit(OpCode opCode, MethodInfo method)
//        {
//            ilCode.Append(opCode, new MethodILInstructionParameter(method), GetStack());
//            il.Emit(opCode, method);
//        }
//
//        private void Emit(OpCode opCode, ConstructorInfo constructor)
//        {
//            ilCode.Append(opCode, new ConstructorILInstructionParameter(constructor), GetStack());
//            il.Emit(opCode, constructor);
//        }
//
//        private readonly Dictionary<Label, Type[]> stacks = new Dictionary<Label, Type[]>();
//
//        private int locals;
//        private int labels;
//
//        private readonly ILCode ilCode = new ILCode();
//        private Stack<Type> stack = new Stack<Type>();
//
//        private readonly ILGenerator il;
//        private readonly bool debugMode;
//        private readonly Type methodReturnType;
//        private readonly Type[] methodParameterTypes;
//        private const string margin = "                                            ";
//    }
//
//    internal abstract class ILInstructionParameter
//    {
//        public abstract string Format();
//    }
//
//    internal class PrimitiveILInstructionParameter : ILInstructionParameter
//    {
//        public PrimitiveILInstructionParameter(object value)
//        {
//            this.value = value;
//        }
//
//        public override string Format()
//        {
//            return value.ToString();
//        }
//
//        private readonly object value;
//    }
//
//    internal class StringILInstructionParameter : ILInstructionParameter
//    {
//        public StringILInstructionParameter(string value)
//        {
//            this.value = value;
//        }
//
//        public override string Format()
//        {
//            return '\'' + value + '\'';
//        }
//
//        private readonly string value;
//    }
//
//    internal class TypeILInstructionParameter : ILInstructionParameter
//    {
//        public TypeILInstructionParameter(Type type)
//        {
//            this.type = type;
//        }
//
//        public override string Format()
//        {
//            return Formatter.Format(type);
//        }
//
//        private readonly Type type;
//    }
//
//    internal class LocalILInstructionParameter : ILInstructionParameter
//    {
//        public LocalILInstructionParameter(GrEmit.Local local)
//        {
//            this.local = local;
//        }
//
//        public override string Format()
//        {
//            return local.Name;
//        }
//
//        private readonly GrEmit.Local local;
//    }
//
//    internal class LabelILInstructionParameter : ILInstructionParameter
//    {
//        public LabelILInstructionParameter(GroboIL.Label label)
//        {
//            this.label = label;
//        }
//
//        public override string Format()
//        {
//            return label.Name;
//        }
//
//        private readonly GroboIL.Label label;
//    }
//
//    internal class MethodILInstructionParameter : ILInstructionParameter
//    {
//        public MethodILInstructionParameter(MethodInfo method)
//        {
//            this.method = method;
//        }
//
//        public override string Format()
//        {
//            return Formatter.Format(method);
//        }
//
//        private readonly MethodInfo method;
//    }
//
//    internal class FieldILInstructionParameter : ILInstructionParameter
//    {
//        public FieldILInstructionParameter(FieldInfo field)
//        {
//            this.field = field;
//        }
//
//        public override string Format()
//        {
//            return Formatter.Format(field);
//        }
//
//        private readonly FieldInfo field;
//    }
//
//    internal class ConstructorILInstructionParameter : ILInstructionParameter
//    {
//        public ConstructorILInstructionParameter(ConstructorInfo constructor)
//        {
//            this.constructor = constructor;
//        }
//
//        public override string Format()
//        {
//            return Formatter.Format(constructor);
//        }
//
//        private readonly ConstructorInfo constructor;
//    }
//
//    internal abstract class ILInstructionComment : ILInstructionParameter
//    {
//    }
//
//    internal class StackILInstructionComment : ILInstructionComment
//    {
//        public StackILInstructionComment(Type[] stack)
//        {
//            this.stack = stack;
//        }
//
//        public override string Format()
//        {
//            return stack == null ? "" : '[' + string.Join(", ", stack.Select(Formatter.Format)) + ']';
//        }
//
//        private readonly Type[] stack;
//    }
//
//    internal class ILCode
//    {
//        public void MarkLabel(GroboIL.Label label)
//        {
//            ilCode.Add(new ILInstruction(default(OpCode), new LabelILInstructionParameter(label), null));
//        }
//
//        public void Append(OpCode opCode, ILInstructionComment comment)
//        {
//            ilCode.Add(new ILInstruction(opCode, null, comment));
//        }
//
//        public void Append(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
//        {
//            ilCode.Add(new ILInstruction(opCode, parameter, comment));
//        }
//
//        private readonly List<ILInstruction> ilCode = new List<ILInstruction>();
//
//        private class ILInstruction
//        {
//            public ILInstruction(OpCode opCode, ILInstructionParameter parameter, ILInstructionComment comment)
//            {
//                OpCode = opCode;
//                Parameter = parameter;
//                Comment = comment;
//            }
//
//            public OpCode OpCode { get; private set; }
//            public ILInstructionParameter Parameter { get; set; }
//            public ILInstructionComment Comment { get; private set; }
//        }
//    }
//}