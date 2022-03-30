<script>
    function copyToClipboard(element) {
      var $temp = $("<input>").trim();
      $("body").append($temp);
      $temp.val($(element).text()).select();
      document.execCommand("copy");
      $temp.remove();
    }
</script> 