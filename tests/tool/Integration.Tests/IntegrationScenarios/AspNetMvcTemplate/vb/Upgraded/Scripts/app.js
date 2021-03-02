angular.module('todoApp', [])
    .controller('TodoListController', function ($http) {
        var todoList = this;

        function init() {
            $http.get('api/tasks').then(function (response) {
                todoList.todos = response.data;
            });
        }

        init();


        todoList.addTodo = function () {
            newTask = { Text: todoList.todoText, Done: false };
            todoList.todos.push(newTask);
            $http.post('api/tasks', newTask);
            todoList.todoText = '';
        };

        todoList.remaining = function () {
            var count = 0;
            angular.forEach(todoList.todos, function (todo) {
                count += todo.Done ? 0 : 1;
            });
            return count;
        };

        todoList.archive = function () {
            var oldTodos = todoList.todos;
            todoList.todos = [];
            angular.forEach(oldTodos, function (todo) {
                if (!todo.Done) {
                    todoList.todos.push(todo);
                }
            });
        };
    });