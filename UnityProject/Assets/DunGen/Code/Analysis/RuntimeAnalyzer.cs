using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DunGen.Analysis;
using DunGen.Graph;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace DunGen.Editor
{
	[AddComponentMenu("DunGen/Analysis/Runtime Analyzer")]
	public sealed class RuntimeAnalyzer : MonoBehaviour
	{
		public DungeonFlow DungeonFlow;
		public int Iterations = 100;
		public int MaxFailedAttempts = 20;
		public bool RunOnStart = true;
		public float MaximumAnalysisTime = 0;

		private DungeonGenerator generator = new DungeonGenerator();
		private GenerationAnalysis analysis;
		private StringBuilder infoText = new StringBuilder();
		private bool finishedEarly;
		private bool prevShouldRandomizeSeed;
		private int targetIterations;
		private int currentIterations { get { return targetIterations - remainingIterations; } }
		private int remainingIterations;
		private Stopwatch analysisTime;
		private bool generateNextFrame;


		private void Start()
		{
			if(RunOnStart)
				Analyze();
		}

		public void Analyze()
		{
			bool isValid = false;

			if(DungeonFlow == null)
				Debug.LogError("No DungeonFlow assigned to analyzer");
			else if(Iterations <= 0)
				Debug.LogError("Iteration count must be greater than 0");
			else if(MaxFailedAttempts <= 0)
				Debug.LogError("Max failed attempt count must be greater than 0");
			else
				isValid = true;

			if(!isValid)
				return;

			prevShouldRandomizeSeed = generator.ShouldRandomizeSeed;

			generator.IsAnalysis = true;
			generator.DungeonFlow = DungeonFlow;
			generator.MaxAttemptCount = MaxFailedAttempts;
			generator.ShouldRandomizeSeed = true;
			analysis = new GenerationAnalysis(Iterations);
			analysisTime = Stopwatch.StartNew();
			remainingIterations = targetIterations = Iterations;

			generator.OnGenerationStatusChanged += OnGenerationStatusChanged;
			generator.Generate();
		}

		private void Update()
		{
			if (MaximumAnalysisTime > 0 && analysisTime.Elapsed.TotalSeconds >= MaximumAnalysisTime)
			{
				remainingIterations = 0;
				finishedEarly = true;
			}

			if (generateNextFrame)
			{
				generateNextFrame = false;
				generator.Generate();
			}
		}

		private void CompleteAnalysis()
		{
			analysisTime.Stop();
			analysis.Analyze();

			UnityUtil.Destroy(generator.Root);
			OnAnalysisComplete();
		}

		private void OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status != GenerationStatus.Complete)
				return;

			analysis.IncrementSuccessCount();
			analysis.Add(generator.GenerationStats);

			remainingIterations--;

			if (remainingIterations <= 0)
			{
				generator.OnGenerationStatusChanged -= OnGenerationStatusChanged;
				CompleteAnalysis();
			}
			else
				generateNextFrame = true;
		}

		private void OnAnalysisComplete()
		{
			const int textPadding = 20;

			void AddInfoEntry(StringBuilder stringBuilder, string title, NumberSetData data)
			{
				string spacing = new string(' ', textPadding - title.Length);
				stringBuilder.Append($"\n\t{title}:{spacing}\t{data}");
			}

			generator.ShouldRandomizeSeed = prevShouldRandomizeSeed;
			infoText.Length = 0;

			if(finishedEarly)
				infoText.AppendLine("[ Reached maximum analysis time before the target number of iterations was reached ]");

			infoText.AppendFormat("Iterations: {0}, Max Failed Attempts: {1}", (finishedEarly) ? analysis.IterationCount : analysis.TargetIterationCount, MaxFailedAttempts);
			infoText.AppendFormat("\nTotal Analysis Time: {0:0.00} seconds", analysisTime.Elapsed.TotalSeconds);
			//infoText.AppendFormat("\n\tOf which spent generating dungeons: {0:0.00} seconds", analysis.AnalysisTime / 1000.0f);
			infoText.AppendFormat("\nDungeons successfully generated: {0}% ({1} failed)", Mathf.RoundToInt(analysis.SuccessPercentage), analysis.TargetIterationCount - analysis.SuccessCount);
			
			infoText.AppendLine();
			infoText.AppendLine();
			
			infoText.Append("## TIME TAKEN (in milliseconds) ##");

			foreach (var step in GenerationAnalysis.MeasurableSteps)
				AddInfoEntry(infoText, step.ToString(), analysis.GetGenerationStepData(step));

			infoText.Append("\n\t-------------------------------------------------------");
			AddInfoEntry(infoText, "Total", analysis.TotalTime);
			
			infoText.AppendLine();
			infoText.AppendLine();
			
			infoText.AppendLine("## ROOM DATA ##");
			AddInfoEntry(infoText, "Main Path Rooms", analysis.MainPathRoomCount);
			AddInfoEntry(infoText, "Branch Path Rooms", analysis.BranchPathRoomCount);
			infoText.Append("\n\t-------------------");
			AddInfoEntry(infoText, "Total", analysis.TotalRoomCount);

			infoText.AppendLine();
			infoText.AppendLine();
			
			infoText.AppendFormat("Retry Count: {0}", analysis.TotalRetries);
		}

		private void OnGUI()
		{
			if(analysis == null || infoText == null || infoText.Length == 0)
			{
				string failedGenerationsCountText = (analysis.SuccessCount < analysis.IterationCount) ? ("\nFailed Dungeons: " + (analysis.IterationCount - analysis.SuccessCount).ToString()) : "";

				GUILayout.Label(string.Format("Analysing... {0} / {1} ({2:0.0}%){3}", currentIterations, targetIterations, (currentIterations / (float)targetIterations) * 100, failedGenerationsCountText));
				return;
			}

			GUILayout.Label(infoText.ToString());
		}
	}
}