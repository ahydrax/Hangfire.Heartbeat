"use strict";

// MODEL

var UtilizationViewModel = function () {
    var self = this;
    self.servers = ko.observableArray();
};

var viewModel = new UtilizationViewModel();

// UPDATER
var updater = function (cpuGraph, memGraph) {
    $.get(document.URL + '/stats',
        function (data) {
            var newServerViews = [];

            for (var i = 0; i < data.length; i++) {
                var current = data[i];

                var server = ko.utils.arrayFirst(viewModel.servers(),
                    function (s) { return s.serverFullName === current.serverFullName; });

                if (server == null) {
                    server = {
                        serverColor: ko.observable("#000000"),
                        serverName: current.serverName,
                        serverFullName: current.serverFullName,
                        processId: ko.observable(current.processId),
                        processName: ko.observable(current.processName),
                        cpuUsage: ko.observable(numeral(current.cpuUsagePercentage).format('0.0') + '%'),
                        ramUsage: ko.observable(numeral(current.workingMemorySet).format('0.00b')),
                        updated: current.timestamp
                    };
                } else {
                    server.processId(current.processId);
                    server.processName(current.processName);
                    server.cpuUsage(current.cpuUsagePercentage + '%');
                    server.ramUsage(numeral(current.workingMemorySet).format('0.00b'));

                    if (current.timestamp === server.updated) continue;

                    server.updated = current.timestamp;
                }
                newServerViews.push(server);

                var x = current.timestamp;

                var cpuGraphData = {};
                cpuGraphData[current.serverFullName] = current.cpuUsagePercentage;
                cpuGraph.series.addData(cpuGraphData, x);

                var memGraphData = {};
                memGraphData[current.serverFullName] = current.workingMemorySet;
                memGraph.series.addData(memGraphData, x);

                var series = ko.utils.arrayFirst(cpuGraph.series,
                    function (s) {
                        return s.name === server.serverFullName;
                    });
                if (series != null) {
                    server.serverColor(series.color);
                }
            }

            cpuGraph.update();
            memGraph.update();
            viewModel.servers(newServerViews);
        });
};

var createGraph = function (elementName, yAxisConfig) {
    var graph = new Rickshaw.Graph({
        element: document.getElementById(elementName),
        height: 400,
        renderer: 'area',
        interpolation: 'cardinal',
        unstack: true,
        stroke: true,
        series: new Rickshaw.Series.FixedDuration([{ name: "stub" }],
            "cool",
            {
                timeInterval: 1000,
                maxDataPoints: 60
            })
    });

    var xAxis = new Rickshaw.Graph.Axis.X({
        graph: graph,
        ticksTreatment: 'glow',
        tickFormat: function (x) { return ''; },
        ticks: 10,
        timeUnit: 'second'
    });
    xAxis.render();

    var yAxis = yAxisConfig(graph);
    yAxis.render();

    return graph;
};

// INITIALIZATION
window.onload = function () {

    var cpuGraph = createGraph("cpu-chart",
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: function (y) { return y !== 0 ? numeral(y).format('0.0') + '%' : ''; },
                ticksTreatment: 'glow'
            });
        });

    var memGraph = createGraph("mem-chart",
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: function (y) { return y !== 0 ? numeral(y).format('0b') : ''; },
                ticksTreatment: 'glow'
            });
        });

    var cpuHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: cpuGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + moment(x * 1000).format() + '</span>';
            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00') + '%' + '<br>' + date;
            return content;
        }
    });

    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + moment(x * 1000).format() + '</span>';
            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00b') + '<br>' + date;
            return content;
        }
    });

    updater(cpuGraph, memGraph);
    setInterval(function () { updater(cpuGraph, memGraph); }, $("#hangfireConfig").data("pollinterval"));
    ko.applyBindings(viewModel);
};
