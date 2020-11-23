﻿// Concept Matrix 3.
// Licensed under the MIT license.

namespace Anamnesis.Files
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Anamnesis.Files.Infos;
	using Anamnesis.Files.Types;
	using Anamnesis.Memory;
	using Anamnesis.PoseModule;
	using Anamnesis.Services;

	#pragma warning disable SA1402, SA1649
	public class PoseFileInfo : JsonFileInfoBase<PoseFile>
	{
		public override string Extension => "pose";
		public override string Name => "Anamnesis Pose File";
		public override IFileSource FileSource => new LocalFileSource("Local Files", SettingsService.Current.DefaultPoseDirectory);
	}

	public class PoseFile : FileBase
	{
		public Configuration Config { get; set; } = new Configuration();

		public Dictionary<string, Bone?>? Bones { get; set; }

		public void WriteToFile(ActorViewModel actor, SkeletonVisual3d skeleton, Configuration config)
		{
			this.Config = config;

			////SkeletonViewModel? skeletonMem = actor?.ModelObject?.Skeleton?.Skeleton;

			if (skeleton == null || skeleton.Bones == null)
				throw new Exception("No skeleton in actor");

			this.Bones = new Dictionary<string, Bone?>();

			foreach ((string name, BoneVisual3d bone) in skeleton.Bones)
			{
				Transform? trans = bone.ViewModel.Model;

				if (trans == null)
					throw new Exception("Bone is missing transform");

				this.Bones.Add(name, new Bone(trans.Value));
			}
		}

		public async Task Apply(ActorViewModel actor, SkeletonVisual3d skeleton, Configuration config)
		{
			SkeletonViewModel? skeletonMem = actor?.ModelObject?.Skeleton?.Skeleton;

			if (skeletonMem == null || skeleton.Bones == null)
				throw new Exception("No skeleton in actor");

			PoseService.Instance.SetEnabled(true);
			PoseService.Instance.CanEdit = false;
			await Task.Delay(100);

			// don't freeze positions if we aren't writing any
			PoseService.Instance.FreezePositions = this.Config.IncludePosition && config.IncludePosition;

			if (this.Bones != null)
			{
				foreach ((string name, Bone? savedBone) in this.Bones)
				{
					if (savedBone == null)
						continue;

					if (!skeleton.Bones.ContainsKey(name))
					{
						Log.Write($"Bone: \"{name}\" not found", "Pose", Log.Severity.Warning);
						continue;
					}

					BoneVisual3d? bone = skeleton.Bones[name];
					TransformViewModel vm = bone.ViewModel;

					if (config.IncludePosition && this.Config.IncludePosition && bone.Position != Vector.Zero)
					{
						vm.Position = savedBone.Position;
					}

					if (config.IncludeRotation && this.Config.IncludeRotation)
					{
						vm.Rotation = savedBone.Rotation;
					}

					if (config.IncludeScale && this.Config.IncludeScale && bone.Scale != Vector.Zero)
					{
						vm.Scale = savedBone.Scale;
					}
				}
			}

			await Task.Delay(100);
			PoseService.Instance.FreezePositions = true;

			await skeletonMem.ReadFromMemoryAsync();
			PoseService.Instance.CanEdit = true;
		}

		public class Configuration
		{
			public List<BoneVisual3d>? Bones;
			public bool IncludeRotation { get; set; } = true;
			public bool IncludePosition { get; set; } = true;
			public bool IncludeScale { get; set; } = true;
		}

		[Serializable]
		public class Bone
		{
			public Bone()
			{
			}

			public Bone(Transform trans)
			{
				this.Position = trans.Position;
				this.Rotation = trans.Rotation;
				this.Scale = trans.Scale;
			}

			public Vector Position { get; set; }
			public Quaternion Rotation { get; set; }
			public Vector Scale { get; set; }
		}
	}
}
