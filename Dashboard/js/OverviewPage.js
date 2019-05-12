"use strict";

function shuffle(arr) {
    var ctr = arr.length, temp, index;
    while (ctr > 0) {
        index = Math.floor(Math.random() * ctr);
        ctr--;
        temp = arr[ctr];
        arr[ctr] = arr[index];
        arr[index] = temp;
    }
    return arr;
}

function ColorGenerator() {
    this._index = 0;
    this._colorsList = shuffle([
        "#4dc9f6",
        "#f67019",
        "#f53794",
        "#537bc4",
        "#acc236",
        "#166a8f",
        "#00a950",
        "#58595b",
        "#8549ba",
        "#85144b",
        "#2ecc40",
        "#39cccc",
        "#ffdc00",
        "#b10dc9",
        "#3d9970",
        "#01ff70",
        "#7fdbff",
        "#f012be",
        "#ff4136",
        "#0074d9",
        "#001f3f",
        "#ff851b"
    ]);
    this._colorCache = {};

    this.getColor = function (name) {
        if (this._colorCache[name] != undefined) {
            return this._colorCache[name];
        }

        var color = "#000000";
        if (this._index < this._colorsList.length) {
            color = this._colorsList[this._index++];

        } else {
            color = getRandomColor();
        }

        this._colorCache[name] = color;
        return color;
    };

    this.getRandomColor = function () {
        var letters = '0123456789ABCDEF'.split('');
        var color = '#';
        for (var i = 0; i < 6; i++) {
            color += letters[Math.floor(Math.random() * 16)];
        }
        return color;
    };
}

var colorGenerator = new ColorGenerator();

// GRAPH
function SeriesGraph(element, tickFormat, pollInterval) {
    this._seriesIndex = 0;
    this._chart = new Chart(element,
        {
            type: 'line',
            data: {
                datasets: [
                ]
            },
            options: {
                aspectRatio: 1,
                scales: {
                    xAxes: [
                        {
                            type: 'realtime',
                            realtime: { duration: 60 * 1000, delay: pollInterval + 1000 },
                            time: {
                                unit: 'second',
                                tooltipFormat: 'LL LTS',
                                displayFormats: { second: 'LTS', minute: 'LTS' }
                            },
                            ticks: {
                                maxRotation: 0
                            }
                        }
                    ],
                    yAxes: [
                        {
                            ticks: {
                                beginAtZero: true,
                                precision: 0,
                                min: 0,
                                maxTicksLimit: 10,
                                suggestedMax: 10,
                                callback: tickFormat
                            }
                        }
                    ]
                },
                elements: { line: { tension: 0 }, point: { radius: 0 } },
                animation: { duration: 0 },
                hover: { animationDuration: 0 },
                responsiveAnimationDuration: 0,
                legend: { display: false },
                tooltips: {
                    mode: 'nearest',
                    intersect: false
                },
                plugins: {
                    filler: {
                        propagate: true
                    }
                }
            }
        });

    this.appendData = function (timestamp, name, data) {
        var now = new Date(timestamp * 1000);

        var server = ko.utils.arrayFirst(this._chart.data.datasets,
            function (s) { return s.id === name; });

        if (server == null) {
            var seriesColor = colorGenerator.getColor(name);
            server = {
                id: name,
                label: getServerShortName(name),
                borderColor: seriesColor,
                backgroundColor: Chart.helpers.color(seriesColor).alpha(0.5).rgbString(),
                fill: "origin",
                data: []
            };
            this._chart.data.datasets.push(server);
            this._seriesIndex++;
        }

        server.data.push({ x: now, y: data });
    };

    this.update = function () {
        this._chart.update();
    }
};

// UTILITY
var formatPercentage = function (value, index, values) {
    return numeral(value).format("0.[00]") + "%";
};

var formatBytes = function (value, index, values) {
    return numeral(value).format("0.[00] b");
};
var getServerShortName = function (name) {
    var lastIndex = name.lastIndexOf(":");
    if (lastIndex != -1) {
        return name.substring(0, lastIndex);
    } else {
        return name;
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
            var newServerViews = [];

            for (var i = 0; i < data.length; i++) {

                var current = data[i];

                cpuGraph.appendData(current.timestamp, current.name, current.cpuUsagePercentage);
                memGraph.appendData(current.timestamp, current.name, current.workingMemorySet);
            }

            cpuGraph.update();
            memGraph.update();

            for (var i = 0; i < data.length; i++) {

                var current = data[i];
                var name = current.name;

                var server = addOrUpdateServerView(name, current);
                server.displayColor(getColor(name, cpuGraph));
                newServerViews.push(server);
            }

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
    var series = ko.utils.arrayFirst(graphSeries._chart.data.datasets,
        function (s) { return s.id === name; });

    return series != null ? series.borderColor : "transparent";
};

// INITIALIZATION
window.onload = function () {
    var updateUrl = $("#heartbeatConfig").data("pollurl");
    var updateInterval = parseInt($("#heartbeatConfig").data("pollinterval"));
    var showFullNameInPopup = $("#heartbeatConfig").data("showfullname") === "true";

    var cpuGraph = new SeriesGraph("cpu-chart", formatPercentage, updateInterval);
    var memGraph = new SeriesGraph("mem-chart", formatBytes, updateInterval);

    updater(cpuGraph, memGraph, updateUrl);
    setInterval(function () { updater(cpuGraph, memGraph, updateUrl); }, updateInterval);
    ko.applyBindings(viewModel);
};
