"use strict";

// MODEL

var UtilizationViewModel = function () {
    var self = this;
    self.servers = ko.observableArray();
};
var viewModel = new UtilizationViewModel();

// UPDATER
var updater = function (cpuGraph, memGraph) {
    $.get($("#heartbeatConfig").data("pollurl"),
        function (data) {
            var cpuGraphData = {};
            var memGraphData = {};
            var newServerViews = [];

            for (var i = 0; i < data.length; i++) {

                var current = data[i];
                var name = current.name;

                var server = getServerView(name, current);
                server.displayColor(getColor(name, cpuGraph.series));

                newServerViews.push(server);

                cpuGraphData[name] = current.cpuUsagePercentage;
                memGraphData[name] = current.workingMemorySet;
            }

            cpuGraph.series.addData(cpuGraphData);
            cpuGraph.update();

            memGraph.series.addData(memGraphData);
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
        series: new Rickshaw.Series.FixedDuration([{ name: "__STUB" }],
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

var getServerView = function (name, current) {

    var server = ko.utils.arrayFirst(viewModel.servers(),
        function (s) { return s.name === name; });

    var cpuUsage = numeral(current.cpuUsagePercentage).format('0.0') + '%';
    var ramUsage = numeral(current.workingMemorySet).format('0.00b');

    if (server == null) {
        server = {
            displayName: current.displayName,
            displayColor: ko.observable("#000000"),
            name: name,
            processId: ko.observable(current.processId),
            processName: ko.observable(current.processName),
            cpuUsage: ko.observable(cpuUsage),
            ramUsage: ko.observable(ramUsage)
        };
    } else {
        server.processId(current.processId);
        server.processName(current.processName);
        server.cpuUsage(cpuUsage);
        server.ramUsage(ramUsage);
    }

    return server;
};

var getColor = function (name, graphSeries) {
    var series = ko.utils.arrayFirst(graphSeries,
        function (s) { return s.name === name; });

    return series != null ? series.color : "#000000";
};

var formatDate = function (unixSeconds) {
    return moment(unixSeconds * 1000).format("H:mm:ss");
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
            var date = '<span class="date">' + formatDate(x) + '</span>';
            if (series.name === "__STUB") return date;

            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00') + '%' + '<br>' + date;
            return content;
        }
    });

    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + formatDate(x) + '</span>';
            if (series.name === "__STUB") return date;

            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00b') + '<br>' + date;
            return content;
        }
    });

    updater(cpuGraph, memGraph);
    setInterval(function () { updater(cpuGraph, memGraph); }, $("#heartbeatConfig").data("pollinterval"));
    ko.applyBindings(viewModel);
};
