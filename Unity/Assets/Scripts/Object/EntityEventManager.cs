﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Base;

namespace Model
{
	[Flags]
	public enum EntityEventType
	{
		Awake = 1,
		Awake1 = 2,
		Awake2 = 4,
		Awake3 = 8,
		Update = 16,
		Load = 32,
		LateUpdate = 64,
	}

	public class EntityTypeInfo
	{
		private readonly Dictionary<EntityEventType, MethodInfo> infos = new Dictionary<EntityEventType, MethodInfo>();

		public void Add(EntityEventType type, MethodInfo methodInfo)
		{
			try
			{
				this.infos.Add(type, methodInfo);
			}
			catch (Exception e)
			{
				throw new Exception($"Add EntityEventType MethodInfo Error: {type}", e);
			}
		}

		public MethodInfo Get(EntityEventType type)
		{
			MethodInfo methodInfo;
			this.infos.TryGetValue(type, out methodInfo);
			return methodInfo;
		}

		public EntityEventType[] GetEntityEventTypes()
		{
			return this.infos.Keys.ToArray();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (EntityEventType disposerEventType in this.infos.Keys.ToArray())
			{
				sb.Append($"{disposerEventType} {this.infos[disposerEventType].Name} ");
			}
			return sb.ToString();
		}
	}

	public sealed class EntityEventManager
	{
		private readonly Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

		private readonly Dictionary<EntityEventType, HashSet<Disposer>> disposers = new Dictionary<EntityEventType, HashSet<Disposer>>();

		private readonly HashSet<Disposer> addDisposers = new HashSet<Disposer>();
		private readonly HashSet<Disposer> removeDisposers = new HashSet<Disposer>();

		private Dictionary<Type, EntityTypeInfo> eventInfo;

		public EntityEventManager()
		{
			foreach (EntityEventType t in Enum.GetValues(typeof(EntityEventType)))
			{
				this.disposers.Add(t, new HashSet<Disposer>());
			}
		}

		public void Register(string name, Assembly assembly)
		{
			this.eventInfo = new Dictionary<Type, EntityTypeInfo>();

			this.assemblies[name] = assembly;

			foreach (Assembly ass in this.assemblies.Values)
			{
				Type[] types = ass.GetTypes();
				foreach (Type type in types)
				{
					object[] attrs = type.GetCustomAttributes(typeof(EntityEventAttribute), true);
					if (attrs.Length == 0)
					{
						continue;
					}

					EntityEventAttribute entityEventAttribute = attrs[0] as EntityEventAttribute;

					Type type2 = entityEventAttribute.ClassType;

					if (!this.eventInfo.ContainsKey(type2))
					{
						this.eventInfo.Add(type2, new EntityTypeInfo());
					}

					foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
					{
						int n = methodInfo.GetParameters().Length;
						if (methodInfo.IsStatic)
						{
							--n;
						}
						string sn = n > 0 ? $"{methodInfo.Name}{n}" : methodInfo.Name;
						foreach (string s in Enum.GetNames(typeof(EntityEventType)))
						{
							if (s != sn)
							{
								continue;
							}
							EntityEventType t = EnumHelper.FromString<EntityEventType>(s);
							this.eventInfo[type2].Add(t, methodInfo);
							break;
						}
					}
				}
			}

			this.Load();
		}

		public void Add(Disposer disposer)
		{
			this.addDisposers.Add(disposer);
		}

		public void Remove(Disposer disposer)
		{
			this.removeDisposers.Add(disposer);
		}

		private void UpdateAddDisposer()
		{
			foreach (Disposer disposer in this.addDisposers)
			{
				EntityTypeInfo entityTypeInfo;
				if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
				{
					continue;
				}

				foreach (EntityEventType disposerEvent2Type in entityTypeInfo.GetEntityEventTypes())
				{
					this.disposers[disposerEvent2Type].Add(disposer);
				}
			}
			this.addDisposers.Clear();
		}

		private void UpdateRemoveDisposer()
		{
			foreach (Disposer disposer in this.removeDisposers)
			{
				EntityTypeInfo entityTypeInfo;
				if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
				{
					continue;
				}

				foreach (EntityEventType disposerEvent2Type in entityTypeInfo.GetEntityEventTypes())
				{
					this.disposers[disposerEvent2Type].Remove(disposer);
				}
			}
			this.removeDisposers.Clear();
		}

		public Assembly GetAssembly(string name)
		{
			return this.assemblies[name];
		}

		public Assembly[] GetAssemblies()
		{
			return this.assemblies.Values.ToArray();
		}

		private void Load()
		{
			HashSet<Disposer> list;
			if (!this.disposers.TryGetValue(EntityEventType.Load, out list))
			{
				return;
			}
			foreach (Disposer disposer in list)
			{
				EntityTypeInfo entityTypeInfo = this.eventInfo[disposer.GetType()];
				entityTypeInfo.Get(EntityEventType.Load).Run(disposer);
			}
		}

		public void Awake(Disposer disposer)
		{
			EntityTypeInfo entityTypeInfo;
			if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
			{
				return;
			}
			entityTypeInfo.Get(EntityEventType.Awake)?.Run(disposer);
		}

		public void Awake(Disposer disposer, object p1)
		{
			EntityTypeInfo entityTypeInfo;
			if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
			{
				return;
			}
			entityTypeInfo.Get(EntityEventType.Awake1)?.Run(disposer, p1);
		}

		public void Awake(Disposer disposer, object p1, object p2)
		{
			EntityTypeInfo entityTypeInfo;
			if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
			{
				return;
			}
			entityTypeInfo.Get(EntityEventType.Awake2)?.Run(disposer, p1, p2);
		}

		public void Awake(Disposer disposer, object p1, object p2, object p3)
		{
			EntityTypeInfo entityTypeInfo;
			if (!this.eventInfo.TryGetValue(disposer.GetType(), out entityTypeInfo))
			{
				return;
			}
			entityTypeInfo.Get(EntityEventType.Awake3)?.Run(disposer, p1, p2, p3);
		}

		public void Update()
		{
			UpdateAddDisposer();
			UpdateRemoveDisposer();
			
			HashSet<Disposer> list;
			if (!this.disposers.TryGetValue(EntityEventType.Update, out list))
			{
				return;
			}
			foreach (Disposer disposer in list)
			{
				try
				{
					if (this.removeDisposers.Contains(disposer))
					{
						continue;
					}
					EntityTypeInfo entityTypeInfo = this.eventInfo[disposer.GetType()];
					entityTypeInfo.Get(EntityEventType.Update).Run(disposer);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public void LateUpdate()
		{
			HashSet<Disposer> list;
			if (!this.disposers.TryGetValue(EntityEventType.LateUpdate, out list))
			{
				return;
			}
			foreach (Disposer disposer in list)
			{
				try
				{
					EntityTypeInfo entityTypeInfo = this.eventInfo[disposer.GetType()];
					entityTypeInfo.Get(EntityEventType.LateUpdate).Run(disposer);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public string MethodInfo()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Type type in this.eventInfo.Keys.ToArray())
			{
				sb.Append($"{type.Name} {this.eventInfo[type]}\n");
			}
			return sb.ToString();
		}
	}
}