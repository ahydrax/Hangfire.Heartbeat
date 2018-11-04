"use strict";

// UTILITY
var formatPercentage = function (x) {
    return numeral(x).format("0.[00]") + "%";
};

var formatBytes = function (x) {
    return numeral(x).format("0.[00] b");
};

var formatDate = function (unixSeconds) {
    return moment(unixSeconds * 1000).format("H:mm:ss");
};

// MODEL
var UtilizationViewModel = function () {
    var self = this;
    self.serverList = ko.observableArray();
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
                
                cpuGraphData[name] = current.cpuUsagePercentage;
                memGraphData[name] = current.workingMemorySet;
            }

            cpuGraph.series.addData(cpuGraphData);
            cpuGraph.update();

            for (var i = 0; i < data.length; i++) {

                var current = data[i];
                var name = current.name;

                var server = addOrUpdateServerView(name, current);
                server.displayColor(getColor(name, cpuGraph.series));
                newServerViews.push(server);
            }

            memGraph.series.addData(memGraphData);
            memGraph.update();

            viewModel.serverList.remove(function (item) {
                var found = false;
                for (var i = 0; i < newServerViews.length; i++) {
                    if (item.name === newServerViews[i].name) {
                        found = true;
                    }
                }
                return !found;
            });

            viewModel.serverList.orderField(viewModel.serverList.orderField());
        });
};

var createGraph = function (elementName, yAxisConfig) {
    var timeInterval = $("#heartbeatConfig").data("pollinterval");
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
                timeInterval: timeInterval,
                maxDataPoints: 60000 / timeInterval
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

var addOrUpdateServerView = function (name, current) {
    var server = ko.utils.arrayFirst(viewModel.serverList(),
        function (s) { return s.name === name; });

    var cpuUsage = formatPercentage(current.cpuUsagePercentage);
    var ramUsage = formatBytes(current.workingMemorySet);

    if (server == null) {
        server = {
            displayName: ko.observable(current.displayName),
            displayColor: ko.observable("#000000"),
            name: name,
            processId: ko.observable(current.processId),
            processName: ko.observable(current.processName),
            cpuUsage: ko.observable(cpuUsage),
            ramUsage: ko.observable(ramUsage)
        };
        viewModel.serverList.push(server);
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


// INITIALIZATION
window.onload = function () {

    var cpuGraph = createGraph("cpu-chart",
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: function (y) { return y !== 0 ? formatPercentage(y) : ''; },
                ticksTreatment: 'glow'
            });
        });

    var memGraph = createGraph("mem-chart",
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: function (y) { return y !== 0 ? formatBytes(y) : ''; },
                ticksTreatment: 'glow'
            });
        });

    var cpuHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: cpuGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + formatDate(x) + '</span>';
            if (series.name === "__STUB") return date;

            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + formatPercentage(y) + '<br>' + date;
            return content;
        }
    });

    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + formatDate(x) + '</span>';
            if (series.name === "__STUB") return date;

            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + formatBytes(y) + '<br>' + date;
            return content;
        }
    });

    updater(cpuGraph, memGraph);
    setInterval(function () { updater(cpuGraph, memGraph); }, $("#heartbeatConfig").data("pollinterval"));
    ko.applyBindings(viewModel);
};
