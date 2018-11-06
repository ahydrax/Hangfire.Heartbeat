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

var formatTicks = function (yAxisFormatter) {
    return function (y) { return y !== 0 ? yAxisFormatter(y) : ""; };
};

var formatServerFullName = function (x) { return x; };

var formatServerShortName = function (serverName) {
    var lastIndex = serverName.lastIndexOf(":");
    if (lastIndex != -1) {
        return serverName.substring(0, lastIndex);
    } else {
        return serverName;
    }
};

var formatDetails = function (serverNameFormatter, yAxisFormatter) {
    return function (series, x, y) {
        var date = "<span class='date'>" + formatDate(x) + "</span>";
        if (series.name === "__STUB") return date;

        var swatch = "<span class='server-indicator' style='background-color: " + series.color + "'></span>&nbsp;";
        var content = swatch + serverNameFormatter(series.name) + ": " + yAxisFormatter(y) + "<br>" + date;
        return content;
    };
};

// MODEL
var UtilizationViewModel = function () {
    var self = this;
    self.serverList = ko.observableArray();
};
var viewModel = new UtilizationViewModel();

// UPDATER
var updater = function (cpuGraph, memGraph, updateUrl) {
    $.get(updateUrl,
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

var createGraph = function (elementName, checkInterval, yAxisConfig) {
    var graph = new Rickshaw.Graph({
        element: document.getElementById(elementName),
        height: 400,
        renderer: "area",
        interpolation: "linear",
        unstack: true,
        stroke: true,
        padding: { top: 0.04 },
        series: new Rickshaw.Series.FixedDuration([{ name: "__STUB" }],
            { scheme: "cool" },
            {
                timeInterval: checkInterval,
                maxDataPoints: 60000 / checkInterval
            })
    });

    var xAxis = new Rickshaw.Graph.Axis.X({
        graph: graph,
        ticksTreatment: "glow",
        tickFormat: function (x) { return ""; },
        ticks: 10,
        timeUnit: "second"
    });
    xAxis.render();

    var yAxis = yAxisConfig(graph);
    yAxis.render();

    $.data(graph.element, "graph", graph);

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
            displayColor: ko.observable("transparent"),
            name: name,
            processId: ko.observable(current.processId),
            processName: ko.observable(current.processName),
            cpuUsage: ko.observable(cpuUsage),
            cpuUsageRawValue: ko.observable(current.cpuUsagePercentage),
            ramUsage: ko.observable(ramUsage),
            ramUsageRawValue: ko.observable(current.workingMemorySet)
        };
        viewModel.serverList.push(server);
    } else {
        server.processId(current.processId);
        server.processName(current.processName);
        server.cpuUsage(cpuUsage);
        server.cpuUsageRawValue(current.cpuUsagePercentage);
        server.ramUsage(ramUsage);
        server.ramUsageRawValue(current.workingMemorySet);
    }

    return server;
};

var getColor = function (name, graphSeries) {
    var series = ko.utils.arrayFirst(graphSeries,
        function (s) { return s.name === name; });

    return series != null ? series.color : "transparent";
};

// INITIALIZATION
window.onload = function () {
    var updateUrl = $("#heartbeatConfig").data("pollurl");
    var updateInterval = $("#heartbeatConfig").data("pollinterval");
    var showFullNameInPopup = $("#heartbeatConfig").data("showfullname") === "true";
    var formatServerName = showFullNameInPopup ? formatServerFullName : formatServerShortName;

    var cpuGraph = createGraph("cpu-chart", updateInterval,
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: formatTicks(formatPercentage),
                ticksTreatment: "glow"
            });
        });
    var cpuHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: cpuGraph,
        formatter: formatDetails(formatServerName, formatPercentage)
    });

    var memGraph = createGraph("mem-chart", updateInterval,
        function (graph) {
            return new Rickshaw.Graph.Axis.Y({
                graph: graph,
                tickFormat: formatTicks(formatBytes),
                ticksTreatment: "glow"
            });
        });
    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        formatter: formatDetails(formatServerName, formatBytes)
    });

    setInterval(function () { updater(cpuGraph, memGraph, updateUrl); }, updateInterval);
    ko.applyBindings(viewModel);

    $(window).on("resize", function () {
        $(".rickshaw_graph").each(function () {
            var container = $(this);
            var graph = container.data("graph");

            if (graph) {
                var width = container.width(),
                    height = container.height();

                if (graph.width !== width || graph.height !== height) {
                    // container size has changed, update graph size
                    graph.setSize({ width: width, height: height });
                    graph.update();
                }
            }
        });
    });
};
