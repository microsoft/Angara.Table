// TaskQueue sequentially executes asynchronous functions queued through the queue() method.
// It allows cancelling all (including running) functions through the clear() method.
// queue() returns a promise which is done when the function execution successfully completes and gets the function's result as an argument.
//                                 is failed when the function execution fails and gets the fail reason as an argument
//                                           or when the function execution is cancelled and gets the value returned by TaskQueue.cancelStatus() as an argument.
(function (TableViewer, $, undefined) {
    TableViewer.TaskQueue = function () {
        // each task is 
        // { 
        //   func is a function () -> promise given by a user,
        //   response is a 'deferred' which allows to indicate the status
        //      response may fail or succeed, if it fails it contains the reason as { reason: R }, where R is 'cancelled' if the task was cancelled
        //                                    if it succeeds it contains the result of the func invocation.
        //   status is one of the following: waiting, running, cancelling, cancelled, completed, failed
        //   priority is a number, tasks with lower value are selected to run
        // }
        var tasks = [];
        var runningTask;
        var cancelStatus = 'Task is cancelled';

        var Task = function (func) {
            return {
                func: func,
                response: $.Deferred(),
                status: 'waiting'
            }
        }

        var tryRunNextTask = function () {
            runningTask = undefined;
            if (tasks.length > 0) // running a task from the queue
            {
                var task = tasks.shift(); // takes first task
                runTask(task);
            }
        }

        var onTaskCancelled = function (task) {
            task.status = 'cancelled';
            task.response.reject(cancelStatus);
        }

        var onTaskFailed = function (task, arg) {
            if (task.status == 'running') {
                task.status = 'failed';
                task.response.reject(arg);
            }
            else if (task.status == 'cancelling') {
                onTaskCancelled(task);
            }
        }

        var onTaskCompleted = function (task, arg) {
            if (task.status == 'running') {
                task.status = 'completed';
                task.response.resolve(arg);
            }
            else if (task.status == 'cancelling') {
                onTaskCancelled(task);
            }
        }

        // Gets the task, changes its state to 'running' and runs it; updates the 'runningTask' variable.
        var runTask = function (task) {
            task.status = 'running';
            runningTask = task;

            try {
                task.func()
                    .done(function (arg) { // task function completed
                        tryRunNextTask();
                        onTaskCompleted(task, arg);
                    })
                    .fail(function (arg) { // task function failed
                        tryRunNextTask();
                        onTaskFailed(task, arg);
                    });
            } catch (e) {
                tryRunNextTask();
                onTaskFailed(task, e);
            }
        }

        return {
            isRunning: function () {
                return runningTask !== undefined;
            },

            taskCount: function () {
                return tasks.length;
            },

            // returns a promise which indicates that the task is complete, failed or cancelled.
            queue: function (func, priority) {
                var task = new Task(func, priority !== undefined ? 1 : priority);
                if (this.isRunning()) {
                    var len = tasks.length;
                    for (var i = 0; i < len; i++)
                    {
                        if (tasks[i].priority > task.priority) {
                            tasks.splice(i, 0, task);
                            break;
                        }
                    }
                    if (len == tasks.length) tasks.push(task);
                }
                else {
                    runTask(task);
                }
                return task.response.promise();
            },

            clear: function () {
                if (runningTask !== undefined) {
                    runningTask.status = 'cancelling';
                    runningTask = undefined;
                }
                if (tasks.length > 0) {
                    for (var i = 0; i < tasks.length; i++) {
                        onTaskCancelled(tasks[i]);
                    }
                    tasks.splice(0, tasks.length);
                }
            },

            // returns a constant string which is passed as an argument to task's fail() continuation when it is cancelled.
            cancelStatus: function () { return cancelStatus; }
        };
    }
}(TableViewer, $));