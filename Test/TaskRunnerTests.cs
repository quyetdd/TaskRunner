#region Usings
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Tasks;

#endregion

namespace Test
{
	[TestFixture]
    public class TaskRunnerTests
	{
		TaskRunner _taskRunner;

        #region TaskImplementation

		class Task : ITask
		{
			public event TasksComplete onComplete;

			public bool isDone { get; private set; }

			public void Execute ()
			{
				isDone = false;

				//wait synchronously for 1 second
				//usually it is an async operation
				IEnumerator e = WaitForOneSecond ();
				while (e.MoveNext());

				isDone = true;

				if (onComplete != null)
					onComplete ();
			}

			private IEnumerator WaitForOneSecond ()
			{
				float time = Time.realtimeSinceStartup;

				while (Time.realtimeSinceStartup - time < 1)
					yield return null;
			}
		}

        #endregion

        #region EnumerableImplementation

		class Enumerable : IEnumerable
		{
			public IEnumerator GetEnumerator ()
			{
				float time = Time.realtimeSinceStartup;

				while (Time.realtimeSinceStartup - time < 1)
					yield return null;
			}
		}

        #endregion

        #region Setup/Teardown

		[SetUp]
		public void InitSources ()
		{
			_taskRunner = (TaskRunner)GameObject.FindObjectOfType (typeof(TaskRunner));
		}

        #endregion

		[Test]
		public void TestSingleTaskExecution ()
		{
			float time = Time.realtimeSinceStartup;
			bool test1Done = false;

			ITask task = new Task ();

			task.onComplete += () => {
				test1Done = true; };

			task.Execute ();

			Assert.That (test1Done == true && task.isDone == true && Time.realtimeSinceStartup - time >= 1);
		}
		
		[Test]
		public void TestSerialTasksExecution ()
		{
			bool allDone = false;

			SerialTasks serialTasks = new SerialTasks ();

			ITask task1 = new Task ();
			ITask task2 = new Task ();

			serialTasks.Add (task1);
			serialTasks.Add (task2);
			
			serialTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (serialTasks);

			Assert.That (allDone == true);
		}
		
		[Test]
		public void TestSerialTaskExecutionOrder1 ()
		{
			bool test2Done = false;
			
			SerialTasks serialTasks = new SerialTasks ();

			ITask task1 = new Task ();
			ITask task2 = new Task ();

			task1.onComplete += () => {
				Assert.That (test2Done == false); };

			task2.onComplete += () => {
				test2Done = true;};
			
			serialTasks.Add (task1);
			serialTasks.Add (task2);
			
			_taskRunner.RunSync (serialTasks);
		}
		
		[Test]
		public void TestSerialTasksExecutionOrder2 ()
		{
			bool test1Done = false;

			SerialTasks serialTasks = new SerialTasks ();

			ITask task1 = new Task ();
			ITask task2 = new Task ();

			task1.onComplete += () => {
				test1Done = true; };

			task2.onComplete += () => {
				Assert.That (test1Done == true); };
			
			serialTasks.Add (task1);
			serialTasks.Add (task2);
			
			_taskRunner.RunSync (serialTasks);
		}

		[Test]
		public void SerialITasks ()
		{
			bool test1Done = false;
			bool test2Done = false;
			bool allDone = false;

			SerialTasks serialTasks = new SerialTasks ();

			ITask task1 = new Task ();
			ITask task2 = new Task ();

			task1.onComplete += () => {
				test1Done = true;
				Assert.That (test2Done == false); };

			task2.onComplete += () => {
				test2Done = true;
				Assert.That (test1Done == true); };
			
			serialTasks.Add (task1);
			serialTasks.Add (task2);
			
			serialTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (serialTasks);

			Assert.That (test1Done == true && test2Done == true && allDone == true);
		}

		[Test]
		public void ParallelITasks ()
		{
			bool test1Done = false;
			bool test2Done = false;
			bool allDone = false;

			ParallelTasks parallelTasks = new ParallelTasks ();

			ITask task = new Task ();

			task.onComplete += () => {
				test1Done = true; };

			parallelTasks.Add (task);

			task = new Task ();

			task.onComplete += () => {
				test2Done = true; };

			parallelTasks.Add (task);
			parallelTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (parallelTasks);

			Assert.That (test1Done, Is.EqualTo (true));
			Assert.That (test2Done, Is.EqualTo (true));
			Assert.That (allDone, Is.EqualTo (true));
		}

		[Test]
		public void SerialEnumerable ()
		{
			bool allDone = false;

			SerialTasks serialTasks = new SerialTasks ();

			Enumerable task = new Enumerable ();

			serialTasks.Add (task);

			task = new Enumerable ();

			serialTasks.Add (task);
			serialTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (serialTasks);

			Assert.That (allDone == true);
		}

		[Test]
		public void ParallelEnumerable ()
		{
			bool allDone = false;

			ParallelTasks parallelTasks = new ParallelTasks ();

			Enumerable task = new Enumerable ();

			parallelTasks.Add (task);

			task = new Enumerable ();

			parallelTasks.Add (task);
			parallelTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (parallelTasks);

			Assert.That (allDone == true);
		}

		[Test]
		public void SerialParallelEnumerable ()
		{
			bool allDone = false;
			bool parallelTasks1Done = false;
			bool parallelTasks2Done = false;

			SerialTasks serialTasks = new SerialTasks ();

			ParallelTasks parallelTasks1 = new ParallelTasks ();

			Enumerable task = new Enumerable ();

			parallelTasks1.Add (task);

			task = new Enumerable ();

			parallelTasks1.Add (task);
			parallelTasks1.onComplete += () => {
				parallelTasks1Done = true;
				Assert.That (parallelTasks2Done == false); };

			ParallelTasks parallelTasks2 = new ParallelTasks ();

			task = new Enumerable ();

			parallelTasks2.Add (task);

			task = new Enumerable ();

			parallelTasks2.Add (task);
			parallelTasks2.onComplete += () => {
				parallelTasks2Done = true;
				Assert.That (parallelTasks1Done == true); };

			serialTasks.Add (parallelTasks1);
			serialTasks.Add (parallelTasks2);

			serialTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (serialTasks);

			Assert.That (parallelTasks1Done == true, "parallelTasks1Done");
			Assert.That (parallelTasks2Done == true, "parallelTasks2Done");
			Assert.That (allDone == true, "allDone");
		}

		[Test]
		public void SerialParallelITask ()
		{
			bool allDone = false;
			bool serialTasks1Done = false;
			bool serialTasks2Done = false;

			ParallelTasks parallelTasks = new ParallelTasks ();

			SerialTasks serialTasks1 = new SerialTasks ();

			ITask task = new Task ();

			serialTasks1.Add (task);

			task = new Task ();

			serialTasks1.Add (task);
			serialTasks1.onComplete += () => {
				serialTasks1Done = true; };

			SerialTasks serialTasks2 = new SerialTasks ();

			task = new Task ();

			serialTasks2.Add (task);

			task = new Task ();

			serialTasks2.Add (task);
			serialTasks2.onComplete += () => {
				serialTasks2Done = true; };

			parallelTasks.Add (serialTasks1);
			parallelTasks.Add (serialTasks2);

			parallelTasks.onComplete += () => {
				allDone = true; };

			_taskRunner.RunSync (parallelTasks);

			Assert.That (serialTasks1Done == true);
			Assert.That (serialTasks2Done == true);
			Assert.That (allDone == true);
		}
	}
}
